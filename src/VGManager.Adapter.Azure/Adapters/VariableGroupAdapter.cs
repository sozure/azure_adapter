using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text.RegularExpressions;
using VGManager.Adapter.Azure.Adapters.Interfaces;
using VGManager.Adapter.Azure.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests.VG;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Adapters;

public class VariableGroupAdapter(
    IHttpClientProvider clientProvider,
    IVariableFilterService variableFilterService,
    ILogger<VariableGroupAdapter> logger
        ) : IVariableGroupAdapter
{
    public async Task<BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse<VariableValue>>>>> GetAllAsync(
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
            logger.LogError(ex, "Couldn't get variables. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyResult<VariableValue>(status));
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            logger.LogError(ex, "Couldn't get variables. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyResult<VariableValue>(status));
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            var status = AdapterStatus.ProjectDoesNotExist;
            logger.LogError(ex, "Couldn't get variables. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyResult<VariableValue>(status));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            logger.LogError(ex, "Couldn't get variables. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyResult<VariableValue>(status));
        }
    }

    public async Task<BaseResponse<AdapterResponseModel<int>>> GetNumberOfFoundVGsAsync(
        GetVGRequest request,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            if (request is null)
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
            logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyCountResult(status));
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyCountResult(status));
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            var status = AdapterStatus.ProjectDoesNotExist;
            logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetEmptyCountResult(status));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            logger.LogError(ex, "Couldn't get variable groups. Status: {status}.", status);
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
            clientProvider.Setup(request.Organization, request.PAT);
            logger.LogDebug("Update variable group with name: {variableGroupName} in {project} Azure project.", variableGroupName, project);
            using var client = await clientProvider.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
            await client!.UpdateVariableGroupAsync(request.VariableGroupId, request.Params, cancellationToken: cancellationToken);
            return ResponseProvider.GetResponse(AdapterStatus.Success);
        }
        catch (VssUnauthorizedException ex)
        {
            var status = AdapterStatus.Unauthorized;
            logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            var status = AdapterStatus.ProjectDoesNotExist;
            logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "An item with the same key has already been added to {variableGroupName}.", variableGroupName);
            return ResponseProvider.GetResponse(AdapterStatus.AlreadyContains);
        }
        catch (TeamFoundationServerInvalidRequestException ex)
        {
            logger.LogError(ex, "Wasn't added to {variableGroupName} because of TeamFoundationServerInvalidRequestException.", variableGroupName);
            return ResponseProvider.GetResponse(AdapterStatus.Unknown);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            logger.LogError(ex, "Couldn't update variable groups. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
    }

    private async Task<BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse<VariableValue>>>>> GetAllVGsAsync(
        GetVGRequest request,
        bool lightWeightRequest,
        CancellationToken cancellationToken
        )
    {
        var project = request.Project;
        clientProvider.Setup(request.Organization, request.PAT);
        logger.LogInformation("Request variable groups from {project} Azure project.", project);
        using var client = await clientProvider.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
        var variableGroups = await client.GetVariableGroupsAsync(project, cancellationToken: cancellationToken);

        var filteredVariableGroups = request.ContainsSecrets ?
                    variableFilterService.Filter(variableGroups, request.VariableGroupFilter) :
                    variableFilterService.FilterWithoutSecrets(request.FilterAsRegex, request.VariableGroupFilter, variableGroups);

        if (request.PotentialVariableGroups is not null)
        {
            filteredVariableGroups = filteredVariableGroups.Where(vg => request.PotentialVariableGroups.Contains(vg.Name));
        }

        var result = CollectResult(request, lightWeightRequest, filteredVariableGroups);
        return ResponseProvider.GetResponse(GetResult(AdapterStatus.Success, result));
    }

    private List<SimplifiedVGResponse<VariableValue>> CollectResult(
        GetVGRequest request,
        bool lightWeightRequest,
        IEnumerable<VariableGroup> filteredVariableGroups
        )
    {
        var result = new List<SimplifiedVGResponse<VariableValue>>();

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
                logger.LogError(ex, "Couldn't parse and create regex. Value: {value}.", request.KeyFilter);
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

    private List<SimplifiedVGResponse<VariableValue>> CollectSubResult(
        Regex regex,
        bool lightWeightRequest,
        IEnumerable<VariableGroup> filteredVariableGroups
        )
    {
        var subResultVar = new List<SimplifiedVGResponse<VariableValue>>();
        foreach (var vg in filteredVariableGroups)
        {
            if (lightWeightRequest)
            {
                var matchedVariables = variableFilterService.Filter(vg.Variables, regex);
                AddToResult(subResultVar, vg, matchedVariables);
            }
            else
            {
                AddToResult(subResultVar, vg, vg.Variables);
            }
        }
        return subResultVar;
    }

    private List<SimplifiedVGResponse<VariableValue>> CollectSubResult(
        string keyFilter,
        bool lightWeightRequest,
        IEnumerable<VariableGroup> filteredVariableGroups
        )
    {
        var subResult = new List<SimplifiedVGResponse<VariableValue>>();
        foreach (var vg in filteredVariableGroups)
        {
            if (lightWeightRequest)
            {
                var matchedVariables = variableFilterService.Filter(vg.Variables, keyFilter);
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
        List<SimplifiedVGResponse<VariableValue>> result,
        VariableGroup vg,
        IEnumerable<KeyValuePair<string, VariableValue>> matchedVariables
        )
    {
        var newDict = new Dictionary<string, VariableValue>(matchedVariables);

        var res = new SimplifiedVGResponse<VariableValue>
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

    private static AdapterResponseModel<IEnumerable<SimplifiedVGResponse<T>>> GetResult<T>(
        AdapterStatus status,
        IEnumerable<SimplifiedVGResponse<T>> variableGroups
        )
    {
        return new()
        {
            Status = status,
            Data = variableGroups
        };
    }

    private static AdapterResponseModel<IEnumerable<SimplifiedVGResponse<T>>> GetEmptyResult<T>(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = Enumerable.Empty<SimplifiedVGResponse<T>>()
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
