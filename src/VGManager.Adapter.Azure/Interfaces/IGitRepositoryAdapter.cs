using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Azure.Entities;

namespace VGManager.Adapter.Azure.Interfaces;

public interface IGitRepositoryAdapter
{
    Task<IEnumerable<GitRepository>> GetAllAsync(
        string organization,
        string project,
        string pat,
        CancellationToken cancellationToken = default
        );

    Task<List<string>> GetVariablesFromConfigAsync(
        GitRepositoryEntity gitRepositoryEntity,
        string pat,
        CancellationToken cancellationToken = default
        );
}
