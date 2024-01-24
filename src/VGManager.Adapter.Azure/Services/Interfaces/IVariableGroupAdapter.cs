using Microsoft.TeamFoundation.DistributedTask.WebApi;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IVariableGroupAdapter
{
    Task<BaseResponse<AdapterResponseModel<IEnumerable<VariableGroup>>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterStatus>> UpdateAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
