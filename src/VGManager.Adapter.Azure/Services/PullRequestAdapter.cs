using Azure.Core;
using Confluent.Kafka;
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

    public async Task<BaseResponse<AdapterResponseModel<List<GitPullRequest>>>> GetPullRequests(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        )
    {
        var payload = PayloadProvider<PRRequest>.GetPayload(command.Payload);

        if (payload is null)
        {
            return ResponseProvider.GetResponse(
                GetFailResponse(new List<GitPullRequest>())
            );
        }

        try
        {
            logger.LogInformation("Request git pull requests from {project} git project.", payload.Repository);
            clientProvider.Setup(payload.Organization, payload.MainPAT);
            using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);

            var searchCriteria = new GitPullRequestSearchCriteria()
            {
                IncludeLinks = true,
                Status = PullRequestStatus.Active,
                SourceRefName = payload.SourceRefName,
                TargetRefName = payload.TargetRefName,
            };

            var prs = await client.GetPullRequestsAsync(
                payload.Project,
                payload.Repository,
                searchCriteria,
                cancellationToken: cancellationToken
                );

            return ResponseProvider.GetResponse(
                new AdapterResponseModel<List<GitPullRequest>>()
                {
                    Data = prs,
                    Status = AdapterStatus.Success
                }
            );
        } catch (Exception ex)
        {
            logger.LogError(ex, "Error getting git pull requests from {project} git project.", payload?.Repository ?? "Unknown");
            return ResponseProvider.GetResponse(
                GetFailResponse(new List<GitPullRequest>())
                );
        }
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
