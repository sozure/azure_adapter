using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using VGManager.Adapter.Azure.Adapters.Interfaces;
using VGManager.Adapter.Azure.Services.Interfaces;
namespace VGManager.Adapter.Azure.Adapters;

public class WorkItemAdapter(
    IHttpClientProvider clientProvider,
    ISprintAdapter sprintAdapter,
    ILogger<WorkItemAdapter> logger
    ) : IWorkItemAdapter
{
    public async Task<WorkItem?> CreateWorkItemAsync(
        string organization,
        string pat,
        string project,
        string repository,
        GitPullRequest pullRequest,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            clientProvider.Setup(organization, pat);
            using var client = await clientProvider.GetClientAsync<WorkItemTrackingHttpClient>(cancellationToken: cancellationToken);
            var sprint = await sprintAdapter.GetCurrentSprintAsync(project, cancellationToken);
            var jsonPatchDocument = BuildJsonPatchDocument(organization, project, sprint.Item2, repository, pullRequest);
            return await client.CreateWorkItemAsync(jsonPatchDocument, project, "Task", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating work item for PR {pullRequestId} in {repository} repository.", pullRequest.PullRequestId, repository);
            return null;
        }
    }

    private static JsonPatchDocument BuildJsonPatchDocument(string organization, string project, string sprint, string repository, GitPullRequest pullRequest)
    {
        var pullRequestId = pullRequest.PullRequestId;
        var url = $"https://dev.azure.com/{organization}/{project}/_git/{repository}/pullRequest/{pullRequestId}";
        return [
                new()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/System.Title",
                    Value = $"Error during pull request ({pullRequestId}) completion in {repository} repository",
                },
                new()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.AssignedTo",
                    Value = $"{pullRequest.CreatedBy.DisplayName}"
                },
                new()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.AreaPath",
                    Value = $"{project}\\backlog"
                },
                new()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.IterationPath",
                    Value = $"{project}\\{sprint}"
                },
                new()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Description",
                    Value = $"Something went wrong during pull request force autocompletion. Please check the followings: <ul><li>Branch policies in affected branches.</li><li><a href=\"{url}\">Check pull request and it's commits.</a></li></ul>",
                }
        ];
    }
}
