using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json.Linq;
using VGManager.Adapter.Azure.Adapters.Interfaces;
using VGManager.Adapter.Azure.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class PullRequestService(
    IGitRepositoryAdapter gitRepositoryAdapter,
    IPullRequestAdapter pullRequestAdapter,
    IWorkItemAdapter workItemAdapter,
    ILogger<PullRequestService> logger
    ) :
    IPullRequestService
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
        var pat = payload.PAT;
        var organization = payload.Organization;

        try
        {
            var loggingMessage = project is null ?
                $"Request git pull requests from {project} azure project." :
                $"Request git pull requests from all azure projects in {organization}.";
            logger.LogInformation(loggingMessage);

            var repositories = await gitRepositoryAdapter.GetAllAsync(organization, pat, project, cancellationToken);
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

            var result = await CollectionPullRequests(organization, pat, project, repositories, searchCriteria, cancellationToken);

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
            var repository = payload.Repository;
            var organization = payload.Organization;
            var pat = payload.PAT;

            logger.LogInformation("Create git pull request for {repository} git repository.", repository);

            var prRequest = new GitPullRequest
            {
                SourceRefName = $"refs/heads/{payload.SourceBranch}",
                TargetRefName = $"refs/heads/{payload.TargetBranch}",
                Title = payload.Title,
                Description = string.Empty
            };

            GitPullRequest measuredPr = null!;

            var pr = await pullRequestAdapter.CreatePullRequestAsync(
                organization,
                pat,
                payload.Project!,
                repository,
                prRequest, 
                cancellationToken
                );

            measuredPr = payload.AutoComplete ? await EnableAutoCompleteOnAnExistingPRAsync(organization, pat, pr, cancellationToken) : pr;

            return ResponseProvider.GetResponse(
                new AdapterResponseModel<bool>()
                {
                    Data = measuredPr is not null,
                    Status = AdapterStatus.Success
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting git pull requests from {project} azure project.", payload.Project ?? "Unknown");
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
            var pat = payload.PAT;
            var sourceBranch = payload.SourceBranch;
            var targetBranch = payload.TargetBranch;

            foreach (var repositoryId in payload.Repositories)
            {
                var branches = await pullRequestAdapter.GetBranchesAsync(
                    organization, 
                    pat, 
                    repositoryId, 
                    cancellationToken
                    );

                var foundSourceBranch = branches.Find(branch => sourceBranch.Contains(branch.Name))?.Name ?? string.Empty;
                var foundTargetBranch = branches.Find(branch => targetBranch.Contains(branch.Name))?.Name ?? string.Empty;

                if (!string.IsNullOrEmpty(foundSourceBranch) && !string.IsNullOrEmpty(foundTargetBranch))
                {
                    var prRequest = new GitPullRequest
                    {
                        SourceRefName = $"refs/heads/{foundSourceBranch}",
                        TargetRefName = $"refs/heads/{foundTargetBranch}",
                        Title = payload.Title,
                        Description = string.Empty
                    };

                    var project = payload.Project;
                    var repository = await gitRepositoryAdapter.GetAsync(organization, pat, repositoryId, cancellationToken);
                    var pr = await pullRequestAdapter.CreatePullRequestAsync(
                        organization, 
                        pat, 
                        project!, 
                        repositoryId, 
                        prRequest, 
                        cancellationToken
                        );

                    if (payload.AutoComplete)
                    {
                        _ = await EnableAutoCompleteOnAnExistingPRAsync(organization, pat, pr, cancellationToken);
                    }
                    else if (payload.ForceComplete)
                    {
                        _ = await CompletePullRequestAsync(
                            organization, 
                            pat, 
                            project!, 
                            repository.Id.ToString(), 
                            foundTargetBranch, 
                            pr, cancellationToken
                            );
                    }
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

    private async Task<List<GitPRResponse>> CollectionPullRequests(
        string organization,
        string pat,
        string? payloadProject,
        List<GitRepository> repositories,
        GitPullRequestSearchCriteria searchCriteria, 
        CancellationToken cancellationToken
        )
    {
        var result = new List<GitPRResponse>();
        foreach (var repository in repositories)
        {
            var prs = await pullRequestAdapter.GetPullRequestsAsync(
                organization,
                pat,
                payloadProject,
                repository.Id,
                searchCriteria,
                cancellationToken
            );

            foreach (var pr in prs)
            {
                var size = await GetPullRequestSizeAsync(organization, pat, repository, pr, cancellationToken);
                (var days, var strAge) = GetPullRequestAge(pr);
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
                    Days = days,
                    Approvers = pr.Reviewers
                        .Where(reviewer => reviewer.Vote == 10 && !reviewer.IsContainer)
                        .Select(reviewer => reviewer.DisplayName)
                        .ToArray()
                });
            }
        }
        var sortedResult = result.OrderBy(pr => pr.Days).ToList();
        return sortedResult;
    }

    private async Task<string> GetPullRequestSizeAsync(
        string organization,
        string pat,
        GitRepository repository,
        GitPullRequest pr,
        CancellationToken cancellationToken
        )
    {
        var commits = await pullRequestAdapter.GetPullRequestCommitsAsync(
            organization,
            pat,
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

    private static (int, string) GetPullRequestAge(GitPullRequest pr)
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

    private async Task<GitPullRequest> EnableAutoCompleteOnAnExistingPRAsync(
        string organization,
        string pat,
        GitPullRequest pullRequest,
        CancellationToken cancellationToken
        )
    {
        var pullRequestWithAutoCompleteEnabled = new GitPullRequest
        {
            AutoCompleteSetBy = new() 
            { 
                Id = pullRequest.CreatedBy.Id, 
                DisplayName = pullRequest.CreatedBy.DisplayName 
            }
        };

        return await pullRequestAdapter.UpdatePullRequestAsync(
            organization,
            pat,
            pullRequestWithAutoCompleteEnabled,
            pullRequest,
            cancellationToken: cancellationToken
        );
    }

    private async Task<GitPullRequest?> CompletePullRequestAsync(
        string organization,
        string pat,
        string project,
        string repository,
        string targetBranch,
        GitPullRequest pullRequest,
        CancellationToken cancellationToken
        )
    {
        try
        {
            await ToggleBranchPoliciesAsync(organization, pat, project!, repository, targetBranch, false, cancellationToken);
            var updatedPr = await EnableAutoCompleteOnAnExistingPRAsync(organization, pat, pullRequest, cancellationToken);
            await Task.Delay(2000, cancellationToken);
            await ToggleBranchPoliciesAsync(organization, pat, project!, repository, targetBranch, true, cancellationToken);
            return updatedPr;
        } catch(Exception ex)
        {
            logger.LogError(ex, "Error during PR {prId} complete in {repository} repository.", pullRequest.PullRequestId, repository);
            _ = await workItemAdapter.CreateWorkItemAsync(organization, pat, project, repository, pullRequest, cancellationToken);
            return null!;
        }
    }

    private async Task ToggleBranchPoliciesAsync(
        string organization,
        string pat,
        string project, 
        string repositoryId, 
        string targetBranch,
        bool policyEnabled,
        CancellationToken cancellationToken
        )
    {
        var policies = await pullRequestAdapter.GetPolicyConfigurationsAsync(
            organization,
            pat,
            project, 
            cancellationToken
            );

        foreach(var policy in policies)
        {
            var scopes = policy.Settings.GetValue("scope") as JArray;
            var scope = scopes?.FirstOrDefault() as JObject;
            var branch = scope?.Value<string>("refName") ?? string.Empty;
            var foundRepositoryId = scope?.Value<string>("repositoryId") ?? string.Empty;

            if(repositoryId == foundRepositoryId && branch == $"refs/heads/{targetBranch}")
            {
                policy.IsEnabled = policyEnabled;
                await pullRequestAdapter.UpdatePolicyConfigurationAsync(
                    organization, 
                    pat, 
                    project, 
                    policy, 
                    cancellationToken
                );
            }
        }
    }

    private static AdapterResponseModel<T> GetFailResponse<T>(T data) where T : notnull
    => new()
        {
            Data = data,
            Status = AdapterStatus.Unknown
        };
}
