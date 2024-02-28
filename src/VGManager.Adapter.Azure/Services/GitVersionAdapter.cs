using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class GitVersionAdapter(
    ISprintAdapter sprintAdapter,
    IHttpClientProvider clientProvider,
    ILogger<GitVersionAdapter> logger
        ) : IGitVersionAdapter
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

            clientProvider.Setup(payload.Organization, payload.PAT);
            logger.LogInformation("Request git branches from {project} git project.", payload.RepositoryId);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            var branches = await client.GetBranchesAsync(payload.RepositoryId, cancellationToken: cancellationToken);
            var result = (AdapterStatus.Success, branches.Select(branch => branch.Name).ToList());
            return ResponseProvider.GetResponse(result);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            logger.LogError(ex, "{project} git project is not found.", payload?.RepositoryId ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>()));
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

            clientProvider.Setup(payload.Organization, payload.PAT);
            logger.LogInformation("Request git tags from {project} git project.", payload.RepositoryId);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            var tags = await client.GetTagRefsAsync(payload.RepositoryId);

            return ResponseProvider.GetResponse((AdapterStatus.Success, tags.Select(tag => tag.Name).ToList()));
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
            logger.LogInformation("Request git tags from {project} git project.", repositoryId);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            
            var sprint = await sprintAdapter.GetCurrentSprintAsync(payload.Project, cancellationToken);
            var branch = await client.GetBranchAsync(
                project,
                repositoryId,
                payload.DefaultBranch,
                cancellationToken: cancellationToken
                );

            var gitAnnotatedTag = new GitAnnotatedTag
            {
                Name = tag,
                Message = string.IsNullOrEmpty(description) ? $"Release {sprint.Item2}" : $"Release {sprint.Item2}: {description}",
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
            logger.LogError(ex, "Error getting git tags from {project} git project.", payload?.RepositoryId);
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, string.Empty));
        }
    }
}
