using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Models;
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

    public void Setup(string organization, string project, string pat)
    {
        _project = project;

        var uriString = $"https://dev.azure.com/{organization}";
        Uri uri;
        Uri.TryCreate(uriString, UriKind.Absolute, out uri!);

        var credentials = new VssBasicCredential(string.Empty, pat);
        _connection = new VssConnection(uri, credentials);
    }

    public async Task<AdapterResponseModel<IEnumerable<VariableGroup>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Request variable groups from {project} Azure project.", _project);
            using var client = await _connection.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
            var variableGroups = await client.GetVariableGroupsAsync(_project, cancellationToken: cancellationToken);
            return GetResult(AdapterStatus.Success, variableGroups);
        }
        catch (VssUnauthorizedException ex)
        {
            var status = AdapterStatus.Unauthorized;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return GetResult(status);
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return GetResult(status);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            var status = AdapterStatus.ProjectDoesNotExist;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return GetResult(status);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return GetResult(status);
        }
    }

    public async Task<AdapterStatus> UpdateAsync(VariableGroupParameters variableGroupParameters, int variableGroupId, CancellationToken cancellationToken = default)
    {
        var variableGroupName = variableGroupParameters.Name;
        variableGroupParameters.VariableGroupProjectReferences = new List<VariableGroupProjectReference>()
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
            await client!.UpdateVariableGroupAsync(variableGroupId, variableGroupParameters, cancellationToken: cancellationToken);
            return AdapterStatus.Success;
        }
        catch (VssUnauthorizedException ex)
        {
            var status = AdapterStatus.Unauthorized;
            _logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return status;
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            _logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return status;
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            var status = AdapterStatus.ProjectDoesNotExist;
            _logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return status;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "An item with the same key has already been added to {variableGroupName}.", variableGroupName);
            return AdapterStatus.AlreadyContains;
        }
        catch (TeamFoundationServerInvalidRequestException ex)
        {
            _logger.LogError(ex, "Wasn't added to {variableGroupName} because of TeamFoundationServerInvalidRequestException.", variableGroupName);
            return AdapterStatus.Unknown;
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return status;
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
