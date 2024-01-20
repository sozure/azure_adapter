using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitVersionAdapter
{
    Task<(AdapterStatus, IEnumerable<string>)> GetBranchesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<(AdapterStatus, IEnumerable<string>)> GetTagsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<(AdapterStatus, string)> CreateTagAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
