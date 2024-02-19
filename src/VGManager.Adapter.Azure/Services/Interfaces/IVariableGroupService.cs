using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IVariableGroupService
{
    Task<BaseResponse<AdapterResponseModel<IEnumerable<SimplifiedVGResponse>>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<BaseResponse<AdapterStatus>> AddVariablesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<BaseResponse<AdapterStatus>> UpdateAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<BaseResponse<AdapterStatus>> DeleteVariablesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<BaseResponse<AdapterResponseModel<int>>> GetNumberOfFoundVGsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
