using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text.RegularExpressions;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests.VG;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.VG;

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

    public async Task<BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>>> GetAllAsync(
        GetVGRequest request,
        bool lightWeightRequest,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            return await GetAllVGsAsync(request, lightWeightRequest, cancellationToken);
        }
        catch (VssUnauthorizedException ex)
        {
            var status = AdapterStatus.Unauthorized;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyResult(status));
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyResult(status));
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            var status = AdapterStatus.ProjectDoesNotExist;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyResult(status));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyResult(status));
        }
    }

    public async Task<BaseResponse<AdapterResponseModel<int>>> GetNumberOfFoundVGsAsync(
        GetVGRequest request,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            if(request is null)
            {
                return ResponseProvider.GetResponse(new AdapterResponseModel<int>()
                {
                    Data = 0,
                    Status = AdapterStatus.Unknown
                });
            }

            var result = await GetAllVGsAsync(request, true, cancellationToken);
            return ResponseProvider.GetResponse(new AdapterResponseModel<int>()
            {
                Data = result.Data.Data.Count(),
                Status = result.Data.Status
            });
        }
        catch (VssUnauthorizedException ex)
        {
            var status = AdapterStatus.Unauthorized;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyCountResult(status));
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyCountResult(status));
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            var status = AdapterStatus.ProjectDoesNotExist;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyCountResult(status));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyCountResult(status));
        }
    }

    public async Task<BaseResponse<AdapterStatus>> UpdateAsync(
        UpdateVGRequest request,
        CancellationToken cancellationToken = default
        )
    {
        if (request is null)
        {
            return ResponseProvider.GetResponse(AdapterStatus.Unknown);
        }

        var variableGroupName = request.Params.Name;
        var project = request.Project;
        request.Params.VariableGroupProjectReferences = new List<VariableGroupProjectReference>()
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
            _clientProvider.Setup(request.Organization, request.PAT);
            _logger.LogDebug("Update variable group with name: {variableGroupName} in {project} Azure project.", variableGroupName, project);
            using var client = await _clientProvider.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
            await client!.UpdateVariableGroupAsync(request.VariableGroupId, request.Params, cancellationToken: cancellationToken);
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

    private async Task<BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>>> GetAllVGsAsync(
        GetVGRequest request,
        bool lightWeightRequest,
        CancellationToken cancellationToken
        )
    {
        var project = request.Project;
        _clientProvider.Setup(request.Organization, request.PAT);
        _logger.LogInformation("Request variable groups from {project} Azure project.", project);
        using var client = await _clientProvider.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
        var variableGroups = await client.GetVariableGroupsAsync(project, cancellationToken: cancellationToken);

        var filteredVariableGroups = request.ContainsSecrets ?
                    _variableFilterService.Filter(variableGroups, request.VariableGroupFilter) :
                    _variableFilterService.FilterWithoutSecrets(request.FilterAsRegex, request.VariableGroupFilter, variableGroups);

        if (request.PotentialVariableGroups is not null)
        {
            filteredVariableGroups = filteredVariableGroups.Where(vg => request.PotentialVariableGroups.Contains(vg.Name));
        }

        var result = CollectResult(request, lightWeightRequest, filteredVariableGroups);
        return ResponseProvider.GetResponse(GetResult(AdapterStatus.Success, result));
    }

    private List<SimplifiedVGResponse> CollectResult(
        GetVGRequest request, 
        bool lightWeightRequest, 
        IEnumerable<VariableGroup> filteredVariableGroups
        )
    {
        var result = new List<SimplifiedVGResponse>();

        if (request.KeyIsRegex ?? false)
        {
            try
            {
                var regex = new Regex(request.KeyFilter.ToLower(), RegexOptions.None, TimeSpan.FromMilliseconds(5000));
                var subResult = CollectSubResult(regex, lightWeightRequest, filteredVariableGroups);
                result.AddRange(subResult);
            }
            catch (RegexParseException ex)
            {
                _logger.LogError(ex, "Couldn't parse and create regex. Value: {value}.", request.KeyFilter);
                var subResult = CollectSubResult(request.KeyFilter, lightWeightRequest, filteredVariableGroups);
                result.AddRange(subResult);
            }
        }
        else
        {
            var subResult = CollectSubResult(request.KeyFilter, lightWeightRequest, filteredVariableGroups);
            result.AddRange(subResult);
        }

        return result;
    }

    private IEnumerable<SimplifiedVGResponse> CollectSubResult(
        Regex regex, 
        bool lightWeightRequest, 
        IEnumerable<VariableGroup> filteredVariableGroups
        )
    {
        var subResult = new List<SimplifiedVGResponse>();
        foreach (var vg in filteredVariableGroups)
        {
            if (lightWeightRequest)
            {
                var matchedVariables = _variableFilterService.Filter(vg.Variables, regex);
                AddToResult(subResult, vg, matchedVariables);
            }
            else
            {
                AddToResult(subResult, vg, vg.Variables);
            }
        }
        return subResult;
    }

    private IEnumerable<SimplifiedVGResponse> CollectSubResult(
        string keyFilter,
        bool lightWeightRequest, 
        IEnumerable<VariableGroup> filteredVariableGroups
        )
    {
        var subResult = new List<SimplifiedVGResponse>();
        foreach (var vg in filteredVariableGroups)
        {
            if (lightWeightRequest)
            {
                var matchedVariables = _variableFilterService.Filter(vg.Variables, keyFilter);
                AddToResult(subResult, vg, matchedVariables);
            }
            else
            {
                AddToResult(subResult, vg, vg.Variables);
            }
        }
        return subResult;
    }

    private static void AddToResult(
        List<SimplifiedVGResponse> result,
        VariableGroup vg,
        IEnumerable<KeyValuePair<string, VariableValue>> matchedVariables
        )
    {
        var newDict = new Dictionary<string, VariableValue>(matchedVariables);
        var res = new SimplifiedVGResponse
        {
            Name = vg.Name,
            Type = vg.Type,
            Id = vg.Id,
            Description = vg.Description,
            Variables = newDict
        };
        if (vg.Type == VariableGroupType.AzureKeyVault)
        {
            var azProviderData = vg.ProviderData as AzureKeyVaultVariableGroupProviderData;
            res.KeyVaultName = azProviderData?.Vault;
        }
        result.Add(res);
    }

    private static AdapterResponseModel<IEnumerable<SimplifiedVGResponse>> GetResult(
        AdapterStatus status,
        IEnumerable<SimplifiedVGResponse> variableGroups
        )
    {
        return new()
        {
            Status = status,
            Data = variableGroups
        };
    }

    private static AdapterResponseModel<IEnumerable<SimplifiedVGResponse>> GetEmptyResult(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = Enumerable.Empty<SimplifiedVGResponse>()
        };
    }

    private static AdapterResponseModel<int> GetEmptyCountResult(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = 0
        };
    }
}
