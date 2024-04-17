using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class PullRequestAdapter(IHttpClientProvider clientProvider, IProfileAdapter profileAdapter, ILogger<PullRequestAdapter> logger) :
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

        var project = payload.Project;
        try
        {
            var loggingMessage = project is null ? 
                $"Request git pull requests from {project} azure project." : 
                $"Request git pull requests from all azure projects in {payload.Organization}.";
            logger.LogInformation(loggingMessage);
            var organization = payload.Organization;

            clientProvider.Setup(organization, payload.PAT);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);

            var repositories = await client.GetRepositoriesAsync(cancellationToken: cancellationToken);
            repositories = repositories.Where(repo => !repo.IsDisabled ?? false).ToList();

            if (project is not null)
            {
                repositories = repositories.Where(repo => repo.ProjectReference.Name == project).ToList();
            }

            var searchCriteria = new GitPullRequestSearchCriteria()
            {
                IncludeLinks = true,
                Status = PullRequestStatus.Active
            };

            var result = await CollectionPullRequests(organization, project, client, repositories, searchCriteria, cancellationToken);

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
            logger.LogError(ex, "Error getting git pull requests from {project} azure project.", project ?? "Unknown");
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

            var profile = await profileAdapter.GetProfileAsync(organization, payload.PAT, cancellationToken);

            if(profile is null)
            {
                logger.LogError("Error getting profile during pull request creation for {repository} git repository.", payload.Repository);
                return ResponseProvider.GetResponse(GetFailResponse(false));
            }

            var prRequest = new GitPullRequest
            {
                SourceRefName = $"refs/heads/{payload.SourceBranch}",
                TargetRefName = $"refs/heads/{payload.TargetBranch}",
                Title = payload.Title,
                Description = "",
                AutoCompleteSetBy = new IdentityRef { Id = profile.Id.ToString(), DisplayName = profile.DisplayName }
            };

            var pr = await client.CreatePullRequestAsync(prRequest, payload.Project, payload.Repository, cancellationToken: cancellationToken);
            var updatedPr = EnableAutoCompleteOnAnExistingPullRequest(client, pr, "Automerged PR");

            return ResponseProvider.GetResponse(
                new AdapterResponseModel<bool>()
                {
                    Data = updatedPr is not null,
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

            var profile = await profileAdapter.GetProfileAsync(organization, payload.PAT, cancellationToken);

            if (profile is null)
            {
                logger.LogError("Error getting profile during pull request creation for repositories.");
                return ResponseProvider.GetResponse(GetFailResponse(false));
            }

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
                        AutoCompleteSetBy = new IdentityRef { Id = profile.Id.ToString(), DisplayName = profile.DisplayName }
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
            var organization = payload.Organization;
            var pat = payload.PAT;
            var profile = await profileAdapter.GetProfileAsync(organization, pat, cancellationToken);

            if(profile is not null)
            {
                var profileId = profile.Id.ToString();
                logger.LogInformation("Accept git pull requests.");

                clientProvider.Setup(organization, pat);
                using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);

                var reviewer = new IdentityRefWithVote
                {
                    Id = profileId,
                    Vote = 10,
                    DisplayName = profile.DisplayName
                };

                foreach (var (repository, prId) in payload.PullRequests)
                {
                    _ = await client.CreatePullRequestReviewerAsync(
                        reviewer: reviewer,
                        project: payload.Project,
                        repositoryId: repository,
                        pullRequestId: prId,
                        reviewerId: profileId,
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

            logger.LogError("Error accepting git pull requests from {project} azure project.", payload.Project);
            return ResponseProvider.GetResponse(
                GetFailResponse(false)
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

    private static GitPullRequest EnableAutoCompleteOnAnExistingPullRequest(
        GitHttpClient gitHttpClient, 
        GitPullRequest pullRequest, 
        string mergeCommitMessage
        )
    {
        var pullRequestWithAutoCompleteEnabled = new GitPullRequest
        {
            AutoCompleteSetBy = new IdentityRef { Id = pullRequest.CreatedBy.Id },
            CompletionOptions = new GitPullRequestCompletionOptions
            {
                MergeStrategy = GitPullRequestMergeStrategy.NoFastForward,
                DeleteSourceBranch = false,
                MergeCommitMessage = mergeCommitMessage
            }
        };

        var updatedPullrequest = gitHttpClient.UpdatePullRequestAsync(
            pullRequestWithAutoCompleteEnabled,
            pullRequest.Repository.Id,
            pullRequest.PullRequestId).Result;

        return updatedPullrequest;
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
