using Microsoft.TeamFoundation.DistributedTask.WebApi;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests.VG;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Adapters.Interfaces;

public interface IVariableGroupAdapter
{
    Task<BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse<VariableValue>>>>> GetAllAsync(
        GetVGRequest request,
        bool lightWeightRequest,
        ExceptionModel[]? exceptions,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterResponseModel<int>>> GetNumberOfFoundVGsAsync(
        GetVGRequest request,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterStatus>> UpdateAsync(
        UpdateVGRequest request,
        CancellationToken cancellationToken = default
        );
}
