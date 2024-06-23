using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Threading;
using VGManager.Adapter.Azure.Adapters.Interfaces;
using VGManager.Adapter.Azure.Services;
using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure.Adapters;

public class GitRepositoryAdapter(
    IHttpClientProvider clientProvider,
    ILogger<GitRepositoryAdapter> logger
    ): IGitRepositoryAdapter
{
    public async Task<List<GitRepository>> GetAllAsync(
        string organization, 
        string pat, 
        string? project, 
        CancellationToken cancellationToken = default
        )
    {
        logger.LogInformation("Request git repositories from {project} azure project.", project);
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
        return project is null || project == "All" ? 
            await client.GetRepositoriesAsync(cancellationToken: cancellationToken) : 
            await client.GetRepositoriesAsync(project: project, cancellationToken: cancellationToken);
    }

    public async Task<GitRepository> GetAsync(
        string organization,
        string pat,
        string repositoryId,
        CancellationToken cancellationToken = default
        )
    {
        logger.LogInformation("Request {id} repository.", repositoryId);
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
        return await client.GetRepositoryAsync(repositoryId, cancellationToken: cancellationToken);
    }

    public async Task<Stream> GetItemTextAsync(
        string organization, 
        string pat, 
        string project, 
        string repositoryId,
        string branch, 
        string filePath, 
        CancellationToken cancellationToken = default
        )
    {
        logger.LogInformation(
            "Requesting configurations from {project} azure project, {repositoryId} git repository.",
            project,
            repositoryId
            );
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
        var gitVersionDescriptor = new GitVersionDescriptor
        {
            VersionType = GitVersionType.Branch,
            Version = branch
        };

        return await client.GetItemTextAsync(
            project: project,
            repositoryId: repositoryId,
            path: filePath,
            versionDescriptor: gitVersionDescriptor,
            cancellationToken: cancellationToken
            );
    }
}
