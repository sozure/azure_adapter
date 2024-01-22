using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Text.Json;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Azure.Services.Requests;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class GitVersionAdapter : IGitVersionAdapter
{
    private readonly ISprintAdapter _sprintAdapter;
    private readonly IHttpClientProvider _clientProvider;
    private readonly ILogger _logger;

    public GitVersionAdapter(
        ISprintAdapter sprintAdapter,
        IHttpClientProvider clientProvider,
        ILogger<GitVersionAdapter> logger
        )
    {
        _sprintAdapter = sprintAdapter;
        _clientProvider = clientProvider;
        _logger = logger;
    }

    public async Task<BaseResponse<Dictionary<string, object>>> GetBranchesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        GitFileBaseRequest<string>? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<GitFileBaseRequest<string>>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
            }

            _clientProvider.Setup(payload.Organization, payload.PAT);
            _logger.LogInformation("Request git branches from {project} git project.", payload.RepositoryId);
            using var client = await _clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            var branches = await client.GetBranchesAsync(payload.RepositoryId, cancellationToken: cancellationToken);
            var result = (AdapterStatus.Success, branches.Select(branch => branch.Name).ToList());
            return ResponseProvider.GetResponse(result);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            _logger.LogError(ex, "{project} git project is not found.", payload?.RepositoryId ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting git branches from {project} git project.", payload?.RepositoryId ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
        }
    }

    public async Task<BaseResponse<Dictionary<string, object>>> GetTagsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        GitFileBaseRequest<Guid>? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<GitFileBaseRequest<Guid>>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
            }

            _clientProvider.Setup(payload.Organization, payload.PAT);
            _logger.LogInformation("Request git tags from {project} git project.", payload.RepositoryId);
            using var client = await _clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            var tags = await client.GetTagRefsAsync(payload.RepositoryId);

            return ResponseProvider.GetResponse((AdapterStatus.Success, tags.Select(tag => tag.Name).ToList()));
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            _logger.LogError(ex, "{project} git project is not found.", payload?.RepositoryId);
            return ResponseProvider.GetResponse((AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting git tags from {project} git project.", payload?.RepositoryId);
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
        }
    }

    public async Task<BaseResponse<Dictionary<string, object>>> CreateTagAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        CreateTagRequest? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<CreateTagRequest>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, string.Empty));
            }

            var repositoryId = payload.RepositoryId;
            var project = payload.Project;
            var tag = payload.TagName;
            _clientProvider.Setup(payload.Organization, payload.PAT);
            _logger.LogInformation("Request git tags from {project} git project.", repositoryId);
            using var client = await _clientProvider.GetClientAsync<GitHttpClient>(cancellationToken);
            var sprint = await _sprintAdapter.GetCurrentSprintAsync(payload.Project, cancellationToken);
            var branch = await client.GetBranchAsync(
                project,
                repositoryId,
                payload.DefaultBranch,
                cancellationToken: cancellationToken
                );

            var gitAnnotatedTag = new GitAnnotatedTag
            {
                Name = tag,
                Message = $"Release {sprint}",
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
            _logger.LogError(ex, "{project} git project is not found.", payload?.RepositoryId);
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, string.Empty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting git tags from {project} git project.", payload?.RepositoryId);
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, string.Empty));
        }
    }
}
