using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Azure.Entities;
using VGManager.Adapter.Models.Kafka;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitRepositoryAdapter
{
    Task<IEnumerable<GitRepository>> GetAllAsync(
        //string organization,
        //string project,
        //string pat,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<List<string>> GetVariablesFromConfigAsync(
        //GitRepositoryEntity gitRepositoryEntity,
        //string pat,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
