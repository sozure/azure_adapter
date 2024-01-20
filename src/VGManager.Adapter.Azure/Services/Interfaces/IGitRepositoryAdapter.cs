using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Models.Kafka;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitRepositoryAdapter
{
    Task<IEnumerable<GitRepository>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<List<string>> GetVariablesFromConfigAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
