using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Text.RegularExpressions;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests.VG;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.VG;

public class VariableGroupService: IVariableGroupService
{
    private readonly IVariableGroupAdapter _variableGroupAdapter;
    private readonly IVariableFilterService _variableFilterService;
    private readonly ILogger _logger;

    public VariableGroupService(
        IVariableGroupAdapter variableGroupAdapter,
        IVariableFilterService variableFilterService,
        ILogger<VariableGroupService> logger
        )
    {
        _variableGroupAdapter = variableGroupAdapter;
        _variableFilterService = variableFilterService;
        _logger = logger;
    }

    public async Task<BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            var payload = PayloadProvider<GetVGRequest>.GetPayload(command.Payload);
            if(payload is null)
            {
                return new BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>>
                {
                    Data = new AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>(),
                };
            }
            return await _variableGroupAdapter.GetAllAsync(payload, cancellationToken);
        } catch (Exception)
        {
            return new BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>>
            {
                Data = new AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>(),
            };
        }
        
    }

    public async Task<BaseResponse<AdapterStatus>> AddVariablesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            var variableGroupAddModel = PayloadProvider<VariableGroupAddModel>.GetPayload(command.Payload);
            if (variableGroupAddModel is null)
            {
                return GetErrorResult();
            }
            variableGroupAddModel.ContainsSecrets = false;
            var vgEntity = await GetAllAsync(variableGroupAddModel, true, cancellationToken);
            var status = vgEntity.Data.Status;

            if (status == AdapterStatus.Success)
            {
                var keyFilter = variableGroupAddModel.KeyFilter;
                var variableGroupFilter = variableGroupAddModel.VariableGroupFilter;
                var key = variableGroupAddModel.Key;
                var value = variableGroupAddModel.Value;
                var filteredVariableGroups = CollectVariableGroups(vgEntity.Data, keyFilter);

                var finalStatus = await AddVariablesAsync(variableGroupAddModel, filteredVariableGroups, key, value, cancellationToken);
                return GetResult(finalStatus);
            }
            return GetResult(status);
        }
        catch (Exception)
        {
            return GetErrorResult();
        }
    }

    public async Task<BaseResponse<AdapterStatus>> UpdateAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            var payload = PayloadProvider<VariableGroupUpdateModel>.GetPayload(command.Payload);
            if (payload is null)
            {
                return GetErrorResult();
            }
            payload.ContainsSecrets = false;
            var vgEntity = await GetAllAsync(payload, payload.FilterAsRegex, cancellationToken);
            var status = vgEntity.Data.Status;

            if (status == AdapterStatus.Success)
            {
                var variableGroupFilter = payload.VariableGroupFilter;
                var keyFilter = payload.KeyFilter;
                var valueFilter = payload.ValueFilter;
                var newValue = payload.NewValue;
                Regex? valueRegex = null;

                if (valueFilter is not null)
                {
                    try
                    {
                        valueRegex = new Regex(valueFilter.ToLower(), RegexOptions.None, TimeSpan.FromMilliseconds(5));
                    }
                    catch (RegexParseException ex)
                    {
                        _logger.LogError(ex, "Couldn't parse and create regex. Value: {value}.", valueFilter);
                    }
                }

                var finalStatus = await UpdateVariableGroupsAsync(
                    payload,
                    newValue,
                    vgEntity.Data.Data,
                    keyFilter,
                    valueRegex,
                    cancellationToken
                    );

                return GetResult(finalStatus);
            }
            return GetResult(status);
        }
        catch (Exception)
        {
            return GetErrorResult();
        }
    }

    public async Task<BaseResponse<AdapterStatus>> DeleteVariablesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            var payload = PayloadProvider<VariableGroupUpdateModel>.GetPayload(command.Payload);
            if (payload is null)
            {
                return GetErrorResult();
            }
            payload.ContainsSecrets = false;
            var vgEntity = await GetAllAsync(payload, payload.FilterAsRegex, cancellationToken);
            var status = vgEntity.Data.Status;

            if (status == AdapterStatus.Success)
            {
                var finalStatus = await DeleteVariablesAsync(payload, vgEntity.Data.Data, cancellationToken);
                return GetResult(finalStatus);
            }

            return GetResult(status);
        }
        catch (Exception)
        {
            return GetErrorResult();
        }
    }
    
    public async Task<BaseResponse<AdapterResponseModel<int>>> GetNumberOfFoundVGsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            var payload = PayloadProvider<GetVGRequest>.GetPayload(command.Payload);
            if (payload is null)
            {
                return GetErrorResult2();
            }
            return await _variableGroupAdapter.GetNumberOfFoundVGsAsync(payload, cancellationToken);
        } catch (Exception)
        {
            return GetErrorResult2();
        }
    }

    private async Task<AdapterStatus> DeleteVariablesAsync(
        VariableGroupModel variableGroupModel,
        IEnumerable<SimplifiedVGResponse> filteredVariableGroups,
        CancellationToken cancellationToken
        )
    {
        var deletionCounter1 = 0;
        var deletionCounter2 = 0;
        var keyFilter = variableGroupModel.KeyFilter;

        foreach (var filteredVariableGroup in filteredVariableGroups)
        {
            var variableGroupName = filteredVariableGroup.Name;

            var deleteIsNeeded = DeleteVariables(
                filteredVariableGroup,
                keyFilter,
                variableGroupModel.ValueFilter
                );

            if (deleteIsNeeded)
            {
                deletionCounter1++;
                var variableGroupParameters = GetVariableGroupParameters(filteredVariableGroup, variableGroupName);
                var updateStatus = await SendUpdateAsync(variableGroupModel, filteredVariableGroup, variableGroupParameters, cancellationToken);

                if (updateStatus == AdapterStatus.Success)
                {
                    deletionCounter2++;
                }
            }
        }
        return deletionCounter1 == deletionCounter2 ? AdapterStatus.Success : AdapterStatus.Unknown;
    }

    private bool DeleteVariables(SimplifiedVGResponse filteredVariableGroup, string keyFilter, string? valueCondition)
    {
        var deleteIsNeeded = false;
        var filteredVariables = _variableFilterService.Filter(filteredVariableGroup.Variables, keyFilter);
        foreach (var filteredVariable in filteredVariables)
        {
            var variableValue = filteredVariable.Value.Value;

            if (valueCondition is not null)
            {
                if (valueCondition.Equals(variableValue))
                {
                    filteredVariableGroup.Variables.Remove(filteredVariable.Key);
                    deleteIsNeeded = true;
                }
            }
            else
            {
                filteredVariableGroup.Variables.Remove(filteredVariable.Key);
                deleteIsNeeded = true;
            }
        }

        return deleteIsNeeded;
    }

    private async Task<AdapterStatus> AddVariablesAsync(
        VariableGroupModel model,
        IEnumerable<SimplifiedVGResponse> filteredVariableGroups,
        string key,
        string value,
        CancellationToken cancellationToken
        )
    {
        var updateCounter = 0;
        var counter = 0;
        foreach (var filteredVariableGroup in filteredVariableGroups)
        {
            counter++;
            try
            {
                var success = await AddVariableAsync(model, key, value, filteredVariableGroup, cancellationToken);

                if (success)
                {
                    updateCounter++;
                }
            }

            catch (ArgumentException ex)
            {
                _logger.LogDebug(
                    ex,
                    "Key has been added previously. Not a breaking error. Variable group: {variableGroupName}, Key: {key}",
                    filteredVariableGroup.Name,
                    key
                    );
                updateCounter++;
            }

            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Something went wrong during variable addition. Variable group: {variableGroupName}, Key: {key}",
                    filteredVariableGroup.Name,
                    key
                    );
            }
        }
        return updateCounter == counter ? AdapterStatus.Success : AdapterStatus.Unknown;
    }

    private async Task<bool> AddVariableAsync(
        VariableGroupModel model,
        string key,
        string value,
        SimplifiedVGResponse filteredVariableGroup,
        CancellationToken cancellationToken
        )
    {
        var variableGroupName = filteredVariableGroup.Name;
        filteredVariableGroup.Variables.Add(key, value);

        var variableGroupParameters = GetVariableGroupParameters(filteredVariableGroup, variableGroupName);
        var updateStatus = await SendUpdateAsync(model, filteredVariableGroup, variableGroupParameters, cancellationToken);

        if (updateStatus == AdapterStatus.Success)
        {
            return true;
        }

        return false;
    }

    private IEnumerable<SimplifiedVGResponse> CollectVariableGroups(
        AdapterResponseModel<IEnumerable<SimplifiedVGResponse>> vgEntity,
        string? keyFilter
        )
    {
        IEnumerable<SimplifiedVGResponse> filteredVariableGroups;
        if (keyFilter is not null)
        {
            try
            {
                var regex = new Regex(keyFilter.ToLower(), RegexOptions.None, TimeSpan.FromMilliseconds(5));

                filteredVariableGroups = vgEntity.Data.Select(vg => vg)
                .Where(vg => vg.Variables.Keys.ToList().FindAll(key => regex.IsMatch(key.ToLower())).Count == 0);
            }
            catch (RegexParseException ex)
            {
                _logger.LogError(ex, "Couldn't parse and create regex. Value: {value}.", keyFilter);
                filteredVariableGroups = vgEntity.Data.Select(vg => vg)
                .Where(vg => vg.Variables.Keys.ToList().FindAll(key => keyFilter.ToLower() == key.ToLower()).Count == 0);
            }
        }
        else
        {
            return vgEntity.Data;
        }

        return filteredVariableGroups;
    }

    private async Task<AdapterStatus> UpdateVariableGroupsAsync(
        VariableGroupModel model,
        string newValue,
        IEnumerable<SimplifiedVGResponse> filteredVariableGroups,
        string keyFilter,
        Regex? valueRegex,
        CancellationToken cancellationToken
        )
    {
        var updateCounter1 = 0;
        var updateCounter2 = 0;

        foreach (var filteredVariableGroup in filteredVariableGroups)
        {
            var variableGroupName = filteredVariableGroup.Name;
            var updateIsNeeded = UpdateVariables(newValue, keyFilter, valueRegex, filteredVariableGroup);

            if (updateIsNeeded)
            {
                updateCounter2++;
                var variableGroupParameters = GetVariableGroupParameters(filteredVariableGroup, variableGroupName);
                var updateStatus = await SendUpdateAsync(model, filteredVariableGroup, variableGroupParameters, cancellationToken);

                if (updateStatus == AdapterStatus.Success)
                {
                    updateCounter1++;
                    _logger.LogDebug("{variableGroupName} updated.", variableGroupName);
                }
            }
        }
        return updateCounter1 == updateCounter2 ? AdapterStatus.Success : AdapterStatus.Unknown;
    }

    private async Task<AdapterStatus> SendUpdateAsync(
        VariableGroupModel model, 
        SimplifiedVGResponse filteredVariableGroup, 
        VariableGroupParameters variableGroupParameters, 
        CancellationToken cancellationToken
        )
    {
        var request = new UpdateVGRequest()
        {
            Organization = model.Organization,
            PAT = model.PAT,
            Project = model.Project,
            VariableGroupId = filteredVariableGroup.Id,
            Params = variableGroupParameters
        };
        var updateStatus = await _variableGroupAdapter.UpdateAsync(request, cancellationToken);
        return updateStatus.Data;
    }

    private bool UpdateVariables(
        string newValue,
        string keyFilter,
        Regex? regex,
        SimplifiedVGResponse filteredVariableGroup
        )
    {
        var filteredVariables = _variableFilterService.Filter(filteredVariableGroup.Variables, keyFilter);
        var updateIsNeeded = false;

        foreach (var filteredVariable in filteredVariables)
        {
            updateIsNeeded = IsUpdateNeeded(filteredVariable, regex, newValue);
        }

        return updateIsNeeded;
    }

    private static bool IsUpdateNeeded(KeyValuePair<string, VariableValue> filteredVariable, Regex? regex, string newValue)
    {
        var variableValue = filteredVariable.Value.Value;

        if (regex is not null)
        {
            if (regex.IsMatch(variableValue.ToLower()))
            {
                filteredVariable.Value.Value = newValue;
                return true;
            }
        }
        else
        {
            filteredVariable.Value.Value = newValue;
            return true;
        }

        return false;
    }

    private static VariableGroupParameters GetVariableGroupParameters(SimplifiedVGResponse filteredVariableGroup, string variableGroupName)
    {
        return new()
        {
            Name = variableGroupName,
            Variables = filteredVariableGroup.Variables,
            Description = filteredVariableGroup.Description,
            Type = filteredVariableGroup.Type,
        };
    }

    private async Task<BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>>> GetAllAsync(
        VariableGroupModel variableGroupModel,
        bool filterAsRegex,
        CancellationToken cancellationToken
        )
    {
        var request = new GetVGRequest()
        {
            Organization = variableGroupModel.Organization,
            PAT = variableGroupModel.PAT,
            Project = variableGroupModel.Project,
            ContainsSecrets = variableGroupModel.ContainsSecrets,
            VariableGroupFilter = variableGroupModel.VariableGroupFilter,
            FilterAsRegex = filterAsRegex,
            KeyIsRegex = variableGroupModel.KeyIsRegex,
            KeyFilter = variableGroupModel.KeyFilter,
        };

        return await _variableGroupAdapter.GetAllAsync(request, cancellationToken);
    }

    private static BaseResponse<AdapterStatus> GetResult(AdapterStatus status)
        => new()
        {
            Data = status
        };

    private static BaseResponse<AdapterStatus> GetErrorResult()
        => new()
        {
            Data = AdapterStatus.Unknown
        };

    private static BaseResponse<AdapterResponseModel<int>> GetErrorResult2()
        => new()
        {
            Data = new AdapterResponseModel<int>
            {
                Data = 0,
                Status = AdapterStatus.Unknown
            }
        };
}
