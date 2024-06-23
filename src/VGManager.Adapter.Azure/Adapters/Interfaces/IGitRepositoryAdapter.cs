using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace VGManager.Adapter.Azure.Adapters.Interfaces;

public interface IGitRepositoryAdapter
{
    Task<List<GitRepository>> GetAllAsync(
        string organization,
        string pat,
        string? project,
        CancellationToken cancellationToken = default
        );
    Task<GitRepository> GetAsync(
        string organization,
        string pat,
        string repositoryId,
        CancellationToken cancellationToken = default
        );
    Task<Stream> GetItemTextAsync(
        string organization,
        string pat,
        string project,
        string repositoryId,
        string branch,
        string filePath,
        CancellationToken cancellationToken = default
        );
}
