using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace VGManager.Adapter.Azure.Adapters.Interfaces;

public interface IWorkItemAdapter
{
    Task<WorkItem?> CreateWorkItemAsync(
        string organization,
        string pat,
        string project,
        string repository,
        GitPullRequest pullRequest,
        CancellationToken cancellationToken = default
        );
}
