using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IReleasePipelineAdapter
{
    Task<BaseResponse<Dictionary<string, object>>> GetEnvironmentsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<BaseResponse<Dictionary<string, object>>> GetVariableGroupsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
