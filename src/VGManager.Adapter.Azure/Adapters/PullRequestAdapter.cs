using Confluent.Kafka;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Organization.Client;
using Microsoft.VisualStudio.Services.TenantPolicy.Client;
using System.IO;
using System.Threading;
using VGManager.Adapter.Azure.Adapters.Interfaces;
using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure.Adapters;

public class PullRequestAdapter(
    IHttpClientProvider clientProvider
    ): IPullRequestAdapter
{
    public async Task<List<GitPullRequest>> GetPullRequestsAsync(
        string organization,
        string pat,
        string? project,
        Guid repositoryId,
        GitPullRequestSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default
        )
    {
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);
        return await client.GetPullRequestsAsync(
            project,
            repositoryId,
            searchCriteria,
            cancellationToken: cancellationToken
        );
    }

    public async Task<List<GitBranchStats>> GetBranchesAsync(
        string organization, 
        string pat, 
        string repository, 
        CancellationToken cancellationToken = default
        )
    {
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);
        return await client.GetBranchesAsync(repository, cancellationToken: cancellationToken);
    }

    public async Task<List<GitCommitRef>> GetPullRequestCommitsAsync(
        string organization, 
        string pat, 
        string project, 
        Guid repositoryId, 
        int pullRequestId, 
        CancellationToken cancellationToken = default
        )
    {
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);
        return await client.GetPullRequestCommitsAsync(
            project,
            repositoryId,
            pullRequestId,
            cancellationToken: cancellationToken
            );
    }

    public async Task<List<PolicyConfiguration>> GetPolicyConfigurationsAsync(
        string organization, 
        string pat, 
        string projectId, 
        CancellationToken cancellationToken = default
        )
    {
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<PolicyHttpClient>(cancellationToken: cancellationToken);
        return await client.GetPolicyConfigurationsAsync(project: projectId, cancellationToken: cancellationToken);
    }

    public async Task<GitPullRequest> CreatePullRequestAsync(
        string organization, 
        string pat, 
        string project, 
        string repository, 
        GitPullRequest prRequest,
        CancellationToken cancellationToken = default
        )
    {
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);
        return await client.CreatePullRequestAsync(prRequest, project, repository, cancellationToken: cancellationToken);
    }

    public async Task<GitPullRequest> UpdatePullRequestAsync(
        string organization,
        string pat,
        GitPullRequest pullRequestWithAutoCompleteEnabled,
        GitPullRequest pullRequest,
        CancellationToken cancellationToken = default
        )
    {
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<GitHttpClient>(cancellationToken: cancellationToken);
        return await client.UpdatePullRequestAsync(
            pullRequestWithAutoCompleteEnabled,
            pullRequest.Repository.Id,
            pullRequest.PullRequestId,
            cancellationToken: cancellationToken
        );
    }

    public async Task UpdatePolicyConfigurationAsync(
        string organization,
        string pat,
        string projectId,
        PolicyConfiguration policy,
        CancellationToken cancellationToken = default
        )
    {
        clientProvider.Setup(organization, pat);
        using var client = await clientProvider.GetClientAsync<PolicyHttpClient>(cancellationToken: cancellationToken);
        _ = await client.UpdatePolicyConfigurationAsync(policy, projectId, policy.Id, cancellationToken: cancellationToken);
    }
}
