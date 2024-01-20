using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitFileAdapter
{
    Task<(AdapterStatus, IEnumerable<string>)> GetFilePathAsync(
        //string organization,
        //string pat,
        //string repositoryId,
        //string fileName,
        //string branch,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<(AdapterStatus, IEnumerable<string>)> GetConfigFilesAsync(
        //string organization,
        //string pat,
        //string repositoryId,
        //string? extension,
        //string branch,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
