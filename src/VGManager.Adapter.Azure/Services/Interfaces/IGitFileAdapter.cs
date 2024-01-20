using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitFileAdapter
{
    Task<BaseResponse<(AdapterStatus, IEnumerable<string>)>> GetFilePathAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<BaseResponse<(AdapterStatus, IEnumerable<string>)>> GetConfigFilesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
