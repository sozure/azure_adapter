using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitVersionService
{
    Task<BaseResponse<Dictionary<string, object>>> GetBranchesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<Dictionary<string, object>>> GetTagsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<Dictionary<string, object>>> GetLatestTagsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<Dictionary<string, object>>> CreateTagAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
