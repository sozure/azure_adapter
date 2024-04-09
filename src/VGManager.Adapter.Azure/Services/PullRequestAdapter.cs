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
        var payload = PayloadProvider<PRRequest>.GetPayload(command.Payload);

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

            var result = new List<GitPRResponse>();

            foreach (var repository in repositories)
            {
                var prs = await client.GetPullRequestsAsync(
                    payload.Project,
                    repository.Id,
                    searchCriteria,
                    cancellationToken: cancellationToken
                );

                foreach(var pr in prs)
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

            return ResponseProvider.GetResponse(
                new AdapterResponseModel<List<GitPRResponse>>()
                {
                    Data = sortedResult,
                    Status = AdapterStatus.Success
                }
            );
        } catch (Exception ex)
        {
            logger.LogError(ex, "Error getting git pull requests from {project} azure project.", payload?.Project ?? "Unknown");
            return ResponseProvider.GetResponse(
                GetFailResponse(new List<GitPRResponse>())
                );
        }
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
