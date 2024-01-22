using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitFileAdapter
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
