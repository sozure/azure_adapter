using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Azure.Entities;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class GitVersionAdapter : IGitVersionAdapter
{
    private readonly IHttpClientProvider _clientProvider;
    private readonly ILogger _logger;

    public GitVersionAdapter(IHttpClientProvider clientProvider, ILogger<GitVersionAdapter> logger)
    {
        _clientProvider = clientProvider;
        _logger = logger;
    }

    public async Task<(AdapterStatus, IEnumerable<string>)> GetBranchesAsync(
        string organization,
        string pat,
        string repositoryId,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            _clientProvider.Setup(organization, pat);
            _logger.LogInformation("Request git branches from {project} git project.", repositoryId);
            using var client = await _clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            var branches = await client.GetBranchesAsync(repositoryId, cancellationToken: cancellationToken);
            return (AdapterStatus.Success, branches.Select(branch => branch.Name).ToList());
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            _logger.LogError(ex, "{project} git project is not found.", repositoryId);
            return (AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting git branches from {project} git project.", repositoryId);
            return (AdapterStatus.Unknown, Enumerable.Empty<string>());
        }
    }

    public async Task<(AdapterStatus, IEnumerable<string>)> GetTagsAsync(
        string organization,
        string pat,
        Guid repositoryId,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            _clientProvider.Setup(organization, pat);
            _logger.LogInformation("Request git tags from {project} git project.", repositoryId);
            using var client = await _clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            var tags = await client.GetTagRefsAsync(repositoryId);

            return (AdapterStatus.Success, tags.Select(tag => tag.Name).ToList());
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            _logger.LogError(ex, "{project} git project is not found.", repositoryId);
            return (AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting git tags from {project} git project.", repositoryId);
            return (AdapterStatus.Unknown, Enumerable.Empty<string>());
        }
    }

    public async Task<(AdapterStatus, string)> CreateTagAsync(
        CreateTagEntity tagEntity,
        string defaultBranch,
        string sprint,
        CancellationToken cancellationToken = default
        )
    {
        var repositoryId = tagEntity.RepositoryId;
        var project = tagEntity.Project;
        try
        {
            var tag = tagEntity.TagName;
            _clientProvider.Setup(tagEntity.Organization, tagEntity.PAT);
            _logger.LogInformation("Request git tags from {project} git project.", repositoryId);
            using var client = await _clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);

            var branch = await client.GetBranchAsync(
                project,
                repositoryId,
                defaultBranch,
                cancellationToken: cancellationToken
                );

            var gitAnnotatedTag = new GitAnnotatedTag
            {
                Name = tag,
                Message = $"Release {sprint}",
                TaggedBy = new GitUserDate
                {
                    Date = DateTime.UtcNow,
                    Name = tagEntity.UserName
                },
                TaggedObject = new GitObject { ObjectId = branch.Commit.CommitId }
            };

            var createdTag = await client.CreateAnnotatedTagAsync(gitAnnotatedTag, project, repositoryId, cancellationToken: cancellationToken);
            return createdTag is not null ? (AdapterStatus.Success, $"refs/tags/{tag}") : (AdapterStatus.Unknown, string.Empty);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            _logger.LogError(ex, "{project} git project is not found.", repositoryId);
            return (AdapterStatus.Unknown, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting git tags from {project} git project.", repositoryId);
            return (AdapterStatus.Unknown, string.Empty);
        }
    }
}
