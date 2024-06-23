using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace VGManager.Adapter.Azure.Adapters.Interfaces;

public interface IPullRequestAdapter
{
    Task<List<GitPullRequest>> GetPullRequestsAsync(
        string organization,
        string pat,
        string? project,
        Guid repositoryId,
        GitPullRequestSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default
        );

    Task<List<GitBranchStats>> GetBranchesAsync(
        string organization,
        string pat,
        string repository,
        CancellationToken cancellationToken = default
        );

    Task<List<GitCommitRef>> GetPullRequestCommitsAsync(
        string organization,
        string pat,
        string project,
        Guid repositoryId,
        int pullRequestId,
        CancellationToken cancellationToken = default
        );

    Task<List<PolicyConfiguration>> GetPolicyConfigurationsAsync(
        string organization,
        string pat,
        string projectId,
        CancellationToken cancellationToken = default
        );

    Task<GitPullRequest> CreatePullRequestAsync(
        string organization,
        string pat,
        string project,
        string repository,
        GitPullRequest prRequest,
        CancellationToken cancellationToken = default
        );

    Task<GitPullRequest> UpdatePullRequestAsync(
        string organization,
        string pat,
        GitPullRequest pullRequestWithAutoCompleteEnabled,
        GitPullRequest pullRequest,
        CancellationToken cancellationToken = default
        );

    Task UpdatePolicyConfigurationAsync(
        string organization,
        string pat,
        string projectId,
        PolicyConfiguration policy,
        CancellationToken cancellationToken = default
        );
}
