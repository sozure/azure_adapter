using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests.VG;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IVariableGroupAdapter
{
    Task<BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>>> GetAllAsync(
        GetVGRequest request,
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
