using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class PullRequestAdapter(IHttpClientProvider clientProvider, ILogger<PullRequestAdapter> logger) :
    IPullRequestAdapter
{

    public async Task<BaseResponse<AdapterResponseModel<List<GitPRResponse>>>> GetPullRequestsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        )
    {
        var payload = PayloadProvider<GitPRRequest>.GetPayload(command.Payload);

        if (payload is null)
        {
            return ResponseProvider.GetResponse(
                GetFailResponse(new List<GitPRResponse>())
            );
        }

        try
        {
            logger.LogInformation("Request git pull requests from {project} azure project.", payload.Project);
            var organization = payload.Organization;

            clientProvider.Setup(organization, payload.PAT);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);

            var repositories = await client.GetRepositoriesAsync(cancellationToken: cancellationToken);
            repositories = repositories.Where(repo => !repo.IsDisabled ?? false).ToList();

            if (payload.Project is not null)
            {
                repositories = repositories.Where(repo => repo.ProjectReference.Name == payload.Project).ToList();
            }

            var searchCriteria = new GitPullRequestSearchCriteria()
            {
                IncludeLinks = true,
                Status = PullRequestStatus.Active
            };

            var result = await CollectionPullRequests(organization, payload.Project, client, repositories, searchCriteria, cancellationToken);

            return ResponseProvider.GetResponse(
                new AdapterResponseModel<List<GitPRResponse>>()
                {
                    Data = result,
                    Status = AdapterStatus.Success
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting git pull requests from {project} azure project.", payload?.Project ?? "Unknown");
            return ResponseProvider.GetResponse(
                GetFailResponse(new List<GitPRResponse>())
                );
        }
    }

    public async Task<BaseResponse<AdapterResponseModel<bool>>> CreatePullRequestAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        )
    {
        var payload = PayloadProvider<CreatePRRequest>.GetPayload(command.Payload);

        if (payload is null)
        {
            return ResponseProvider.GetResponse(
                GetFailResponse(false)
            );
        }

        try
        {
            logger.LogInformation("Create git pull request for {repository} git repository.", payload.Repository);
            var organization = payload.Organization;

            clientProvider.Setup(organization, payload.PAT);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);

            var prRequest = new GitPullRequest
            {
                SourceRefName = payload.SourceBranch,
                TargetRefName = payload.TargetBranch,
                Title = payload.Title,
                Description = "",
                Status = PullRequestStatus.Completed
            };

            var pr = await client.CreatePullRequestAsync(prRequest, payload.Project, payload.Repository, cancellationToken: cancellationToken);

            return ResponseProvider.GetResponse(
                new AdapterResponseModel<bool>()
                {
                    Data = pr is not null,
                    Status = AdapterStatus.Success
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting git pull requests from {project} azure project.", payload?.Project ?? "Unknown");
            return ResponseProvider.GetResponse(
                GetFailResponse(false)
            );
        }
    }

    public async Task<BaseResponse<AdapterResponseModel<bool>>> CreatePullRequestsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        )
    {
        var payload = PayloadProvider<CreatePRsRequest>.GetPayload(command.Payload);

        if (payload is null)
        {
            return ResponseProvider.GetResponse(
                GetFailResponse(false)
            );
        }

        try
        {
            logger.LogInformation("Create git pull requests.");
            var organization = payload.Organization;

            clientProvider.Setup(organization, payload.PAT);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);

            foreach (var repository in payload.Repositories)
            {
                var branches = await client.GetBranchesAsync(repository, cancellationToken: cancellationToken);

                var sourceBranch = branches.Find(branch => payload.SourceBranch.Contains(branch.Name))?.Name ?? string.Empty;
                var targetBranch = branches.Find(branch => payload.TargetBranch.Contains(branch.Name))?.Name ?? string.Empty;

                if (!string.IsNullOrEmpty(sourceBranch) && !string.IsNullOrEmpty(targetBranch))
                {
                    var prRequest = new GitPullRequest
                    {
                        SourceRefName = $"refs/heads/{sourceBranch}",
                        TargetRefName = $"refs/heads/{targetBranch}",
                        Title = payload.Title,
                        Description = "",
                        Status = PullRequestStatus.Completed
                    };

                    _ = await client.CreatePullRequestAsync(prRequest, payload.Project, repository, cancellationToken: cancellationToken);
                }
            }

            return ResponseProvider.GetResponse(
                new AdapterResponseModel<bool>()
                {
                    Data = true,
                    Status = AdapterStatus.Success
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting git pull requests from {project} azure project.", payload?.Project ?? "Unknown");
            return ResponseProvider.GetResponse(
                GetFailResponse(false)
            );
        }
    }

    public async Task<BaseResponse<AdapterResponseModel<bool>>> ApprovePullRequestsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        )
    {
        var payload = PayloadProvider<ApprovePRsRequest>.GetPayload(command.Payload);

        if (payload is null)
        {
            return ResponseProvider.GetResponse(
                GetFailResponse(false)
            );
        }

        try
        {
            logger.LogInformation("Accept git pull requests.");
            var organization = payload.Organization;

            clientProvider.Setup(organization, payload.PAT);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);

            var approverId = payload.ApproverId;
            var approverName = payload.Approver;

            var reviewer = new IdentityRefWithVote
            {
                Id = approverId,
                Vote = 10,
                DisplayName = approverName,
            };

            foreach (var (repository, prId) in payload.PullRequests)
            {
                _ = await client.CreatePullRequestReviewerAsync(
                    reviewer,
                    repository,
                    prId,
                    approverId,
                    cancellationToken: cancellationToken
                    );
            }

            return ResponseProvider.GetResponse(
                new AdapterResponseModel<bool>()
                {
                    Data = true,
                    Status = AdapterStatus.Success
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error accepting git pull requests from {project} azure project.", payload.Project);
            return ResponseProvider.GetResponse(
                GetFailResponse(false)
            );
        }
    }

    private static async Task<List<GitPRResponse>> CollectionPullRequests(
        string organization,
        string? payloadProject,
        GitHttpClient client,
        List<GitRepository> repositories,
        GitPullRequestSearchCriteria searchCriteria, CancellationToken cancellationToken
        )
    {
        var result = new List<GitPRResponse>();
        foreach (var repository in repositories)
        {
            var prs = await client.GetPullRequestsAsync(
                payloadProject,
                repository.Id,
                searchCriteria,
                cancellationToken: cancellationToken
            );

            foreach (var pr in prs)
            {
                var size = await GetPRSize(client, repository, pr, cancellationToken);
                (var days, var strAge) = GetPRAge(pr);
                var project = repository.ProjectReference.Name;
                result.Add(new()
                {
                    Title = pr.Title,
                    Repository = repository.Name,
                    Url = $"https://dev.azure.com/{organization}/{project}/_git/{pr.Repository.Id}/pullRequest/{pr.PullRequestId}",
                    CreatedBy = pr.CreatedBy.DisplayName,
                    Project = project,
                    Created = strAge,
                    Size = size,
                    Days = days
                });
            }
        }
        var sortedResult = result.OrderBy(pr => pr.Days).ToList();
        return sortedResult;
    }

    private static async Task<string> GetPRSize(
        GitHttpClient client,
        GitRepository repository,
        GitPullRequest pr,
        CancellationToken cancellationToken
        )
    {
        var commits = await client.GetPullRequestCommitsAsync(
            pr.Repository.ProjectReference.Name,
            repository.Id,
            pr.PullRequestId,
            cancellationToken: cancellationToken
            );

        var commitCounter = commits?.Count ?? 0;

        return commitCounter switch
        {
            var count when count < 3 => "Small",
            var count when count < 10 => "Medium",
            _ => "Large",
        };
    }

    private static (int, string) GetPRAge(GitPullRequest pr)
    {
        var difference = pr.CreationDate - DateTime.Now;
        var days = difference.Days * -1;

        return (days, days switch
        {
            var d when d > 1 => $"{days} days ago",
            1 => "Yesterday",
            _ => "Today",
        });
    }

    private static AdapterResponseModel<T> GetFailResponse<T>(T data) where T : notnull
    {
        return new AdapterResponseModel<T>()
        {
            Data = data,
            Status = AdapterStatus.Unknown
        };
    }
}
