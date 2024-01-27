using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class VariableGroupAdapter : IVariableGroupAdapter
{
    private readonly IHttpClientProvider _clientProvider;
    private readonly IVariableFilterService _variableFilterService;
    private readonly ILogger _logger;

    public VariableGroupAdapter(
        IHttpClientProvider clientProvider,
        IVariableFilterService variableFilterService,
        ILogger<VariableGroupAdapter> logger
        )
    {
        _clientProvider = clientProvider;
        _variableFilterService = variableFilterService;
        _logger = logger;
    }

    public async Task<BaseResponse<AdapterResponseModel<IEnumerable<VariableGroup>>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<GetVGRequest>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                var status = AdapterStatus.Unknown;
                return ResponseProvider.GetResponse(GetResult(status));
            }
            var project = payload.Project;
            _clientProvider.Setup(payload.Organization, payload.PAT);
            _logger.LogInformation("Request variable groups from {project} Azure project.", project);
            using var client = await _clientProvider.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
            var variableGroups = await client.GetVariableGroupsAsync(project, cancellationToken: cancellationToken);

            var filteredVariableGroups = payload.ContainsSecrets ?
                        _variableFilterService.Filter(variableGroups, payload.VariableGroupFilter) :
                        _variableFilterService.FilterWithoutSecrets(true, payload.VariableGroupFilter, variableGroups);

            return ResponseProvider.GetResponse(GetResult(AdapterStatus.Success, filteredVariableGroups));
        }
        catch (VssUnauthorizedException ex)
        {
            var status = AdapterStatus.Unauthorized;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetResult(status));
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetResult(status));
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            var status = AdapterStatus.ProjectDoesNotExist;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetResult(status));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetResult(status));
        }
    }

    public async Task<BaseResponse<AdapterStatus>> UpdateAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<UpdateVGRequest>.GetPayload(command.Payload);
        if (payload is null)
        {
            return ResponseProvider.GetResponse(AdapterStatus.Unknown);
        }

        var variableGroupName = payload.Params.Name;
        var project = payload.Project;
        payload.Params.VariableGroupProjectReferences = new List<VariableGroupProjectReference>()
        {
            new()
            {
                Name = variableGroupName,
                ProjectReference = new()
                {
                    Name = project
                }
            }
        };

        try
        {
            _clientProvider.Setup(payload.Organization, payload.PAT);
            _logger.LogDebug("Update variable group with name: {variableGroupName} in {project} Azure project.", variableGroupName, project);
            using var client = await _clientProvider.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
            await client!.UpdateVariableGroupAsync(payload.VariableGroupId, payload.Params, cancellationToken: cancellationToken);
            return ResponseProvider.GetResponse(AdapterStatus.Success);
        }
        catch (VssUnauthorizedException ex)
        {
            var status = AdapterStatus.Unauthorized;
            _logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            _logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            var status = AdapterStatus.ProjectDoesNotExist;
            _logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "An item with the same key has already been added to {variableGroupName}.", variableGroupName);
            return ResponseProvider.GetResponse(AdapterStatus.AlreadyContains);
        }
        catch (TeamFoundationServerInvalidRequestException ex)
        {
            _logger.LogError(ex, "Wasn't added to {variableGroupName} because of TeamFoundationServerInvalidRequestException.", variableGroupName);
            return ResponseProvider.GetResponse(AdapterStatus.Unknown);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
    }

    private static AdapterResponseModel<IEnumerable<VariableGroup>> GetResult(AdapterStatus status, IEnumerable<VariableGroup> variableGroups)
    {
        return new()
        {
            Status = status,
            Data = variableGroups
        };
    }

    private static AdapterResponseModel<IEnumerable<VariableGroup>> GetResult(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = Enumerable.Empty<VariableGroup>()
        };
    }
}
