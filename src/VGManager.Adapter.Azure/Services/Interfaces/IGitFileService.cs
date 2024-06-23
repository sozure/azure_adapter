using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitFileService
{
    Task<BaseResponse<Dictionary<string, object>>> GetFilePathAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<BaseResponse<Dictionary<string, object>>> GetConfigFilesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
