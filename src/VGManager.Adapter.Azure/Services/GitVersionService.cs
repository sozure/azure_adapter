using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text.RegularExpressions;
using VGManager.Adapter.Azure.Adapters.Interfaces;
using VGManager.Adapter.Azure.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class GitVersionService(
    ISprintAdapter sprintAdapter,
    IHttpClientProvider clientProvider,
    ILogger<GitVersionService> logger
        ) : IGitVersionService
{
    public async Task<BaseResponse<Dictionary<string, object>>> GetBranchesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<GitFileBaseRequest<string>>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
            }
            var branches = await GetBranchesAsync(payload.Organization, payload.PAT, payload.RepositoryId, cancellationToken);
            var result = (AdapterStatus.Success, branches);
            return ResponseProvider.GetResponse(result);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            logger.LogError(ex, "{project} git project is not found.", payload?.RepositoryId ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>()));
        }
        catch (VssServiceResponseException ex)
        {
            logger.LogError(ex, "Error getting git branches from {project} git project.", payload?.RepositoryId ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.BranchesDoNotExist, Enumerable.Empty<string>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting git branches from {project} git project.", payload?.RepositoryId ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
        }
    }

    public async Task<BaseResponse<Dictionary<string, object>>> GetTagsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<GitFileBaseRequest<Guid>>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
            }

            var tags = await GetTagsAsync(payload.Organization, payload.PAT, payload.RepositoryId, cancellationToken);
            return ResponseProvider.GetResponse((AdapterStatus.Success, tags));
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            logger.LogError(ex, "{project} git project is not found.", payload?.RepositoryId);
            return ResponseProvider.GetResponse((AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting git tags from {project} git project.", payload?.RepositoryId);
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
        }
    }

    public async Task<BaseResponse<Dictionary<string, object>>> CreateTagAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<CreateTagRequest>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, string.Empty));
            }

            var repositoryId = payload.RepositoryId;
            var project = payload.Project;
            var tag = payload.TagName;
            var description = payload.Description;
            clientProvider.Setup(payload.Organization, payload.PAT);
            logger.LogInformation("Create git tag for {project} git project.", repositoryId);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);

            var sprint = await sprintAdapter.GetCurrentSprintAsync(payload.Project, cancellationToken);
            var branch = await client.GetBranchAsync(
                project,
                repositoryId,
                payload.DefaultBranch,
                cancellationToken: cancellationToken
                );

            var sprintNumber = sprint.Item2.ToLower();

            var gitAnnotatedTag = new GitAnnotatedTag
            {
                Name = tag,
                Message = string.IsNullOrEmpty(description) ? $"Release {sprintNumber}" : $"Release {sprintNumber}: {description}",
                TaggedBy = new GitUserDate
                {
                    Date = DateTime.UtcNow,
                    Name = payload.UserName
                },
                TaggedObject = new GitObject { ObjectId = branch.Commit.CommitId }
            };

            var createdTag = await client.CreateAnnotatedTagAsync(gitAnnotatedTag, project, repositoryId, cancellationToken: cancellationToken);
            return ResponseProvider.GetResponse(
                createdTag is not null ?
                (AdapterStatus.Success, $"refs/tags/{tag}") :
                (AdapterStatus.Unknown, string.Empty)
                );
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            logger.LogError(ex, "{project} git project is not found.", payload?.RepositoryId);
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, string.Empty));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating git tag for {project} git project.", payload?.RepositoryId);
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, string.Empty));
        }
    }

    public async Task<BaseResponse<Dictionary<string, object>>> GetLatestTagsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<GitLatestTagsRequest>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, new Dictionary<string, string>()));
            }

            var result = new Dictionary<string, string>();
            var regex = new Regex(@"^refs/tags/\d+\.\d+\.\d+$");

            foreach (var repositoryId in payload.RepositoryIds)
            {
                var tags = await GetTagsAsync(payload.Organization, payload.PAT, repositoryId, cancellationToken);
                var filteredTags = tags.Where(tag => regex.IsMatch(tag)).ToList();
                filteredTags.Sort(new VersionComparer());
                var latestTag = filteredTags.LastOrDefault();
                if (latestTag is not null)
                {
                    result.Add(repositoryId.ToString(), latestTag.Replace("refs/tags/", string.Empty));
                }
            }

            return ResponseProvider.GetResponse((AdapterStatus.Success, result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting latest tags.");
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, new Dictionary<string, string>()));
        }
    }

    private async Task<IEnumerable<string>> GetBranchesAsync(
        string organization,
        string pat,
        string repositoryId,
        CancellationToken cancellationToken
        )
    {
        clientProvider.Setup(organization, pat);
        logger.LogInformation("Request git branches from {project} git project.", repositoryId);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
        var branches = await client.GetBranchesAsync(repositoryId, cancellationToken: cancellationToken);
        return branches.Select(branch => branch.Name).ToList();
    }

    private async Task<List<string>> GetTagsAsync(
        string organization,
        string pat,
        Guid repositoryId,
        CancellationToken cancellationToken
        )
    {
        clientProvider.Setup(organization, pat);
        logger.LogInformation("Request git tags from {project} git project.", repositoryId);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
        var tags = await client.GetTagRefsAsync(repositoryId);
        return tags.Select(tag => tag.Name).ToList();
    }

    public class VersionComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x is null && y is null)
            {
                return 0;
            }
            else if (x is null)
            {
                return -1;
            }
            else if (y is null)
            {
                return 1;
            }

            var versionX = ParseVersion(x);
            var versionY = ParseVersion(y);

            return versionX.CompareTo(versionY);
        }

        private static Version ParseVersion(string versionString)
        {
            var versionNumber = versionString[(versionString.LastIndexOf('/') + 1)..];
            return new Version(versionNumber);
        }
    }
}
