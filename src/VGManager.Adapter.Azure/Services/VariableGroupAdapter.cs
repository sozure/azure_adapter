using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text.Json;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Azure.Services.Requests;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class VariableGroupAdapter : IVariableGroupAdapter
{
    private VssConnection _connection = null!;
    private string _project = null!;
    private readonly ILogger _logger;

    public VariableGroupAdapter(ILogger<VariableGroupAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<BaseResponse<AdapterResponseModel<IEnumerable<VariableGroup>>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        ExtendedBaseRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ExtendedBaseRequest>(command.Payload);

            if (payload is null)
            {
                var status = AdapterStatus.Unknown;
                return ResponseProvider.GetResponse(GetResult(status));
            }

            Setup(payload.Organization, payload.Project, payload.PAT);
            _logger.LogInformation("Request variable groups from {project} Azure project.", _project);
            using var client = await _connection.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
            var variableGroups = await client.GetVariableGroupsAsync(_project, cancellationToken: cancellationToken);
            return ResponseProvider.GetResponse(GetResult(AdapterStatus.Success, variableGroups));
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
        VGRequest? payload;
        payload = JsonSerializer.Deserialize<VGRequest>(command.Payload);

        if (payload is null)
        {
            return ResponseProvider.GetResponse(AdapterStatus.Unknown);
        }

        var variableGroupName = payload.Params.Name;
        payload.Params.VariableGroupProjectReferences = new List<VariableGroupProjectReference>()
        {
            new()
            {
                Name = variableGroupName,
                ProjectReference = new()
                {
                    Name = _project
                }
            }
        };

        try
        {
            _logger.LogDebug("Update variable group with name: {variableGroupName} in {project} Azure project.", variableGroupName, _project);
            using var client = await _connection.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
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

    private void Setup(
        string organization,
        string project,
        string pat
        )
    {
        _project = project;

        var uriString = $"https://dev.azure.com/{organization}";
        Uri uri;
        Uri.TryCreate(uriString, UriKind.Absolute, out uri!);

        var credentials = new VssBasicCredential(string.Empty, pat);
        _connection = new VssConnection(uri, credentials);
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
