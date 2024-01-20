using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class BuildPipelineAdapter : IBuildPipelineAdapter
{
    private readonly IHttpClientProvider _clientProvider;
    private readonly ILogger _logger;

    public BuildPipelineAdapter(IHttpClientProvider clientProvider, ILogger<BuildPipelineAdapter> logger)
    {
        _clientProvider = clientProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<BuildDefinitionReference>> GetBuildPipelinesAsync(
        string organization,
        string pat,
        string project,
        CancellationToken cancellationToken = default
        )
    {
        _logger.LogInformation("Request build pipelines from Azure DevOps.");
        _clientProvider.Setup(organization, pat);
        using var client = await _clientProvider.GetClientAsync<BuildHttpClient>(cancellationToken);
        return await client.GetDefinitionsAsync(project, cancellationToken: cancellationToken);
    }

    public async Task<BuildDefinitionReference> GetBuildPipelineAsync(
        string organization,
        string pat,
        string project,
        int id,
        CancellationToken cancellationToken = default
        )
    {
        _logger.LogInformation("Request build pipelines from Azure DevOps.");
        _clientProvider.Setup(organization, pat);
        using var client = await _clientProvider.GetClientAsync<BuildHttpClient>(cancellationToken);
        return await client.GetDefinitionAsync(project, id, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Run build pipeline
    /// </summary>
    /// <param name="organization"></param>
    /// <param name="pat"></param>
    /// <param name="project"></param>
    /// <param name="definitionId"></param>
    /// <param name="sourceBranch">for example: "refs/tags/2.1.1" or "refs/heads/develop"</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AdapterStatus> RunBuildPipelineAsync(
        string organization,
        string pat,
        string project,
        int definitionId,
        string sourceBranch,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            _logger.LogInformation("Request build pipelines from Azure DevOps.");
            _clientProvider.Setup(organization, pat);
            using var client = await _clientProvider.GetClientAsync<BuildHttpClient>(cancellationToken);
            var pipeline = await client.GetDefinitionAsync(project, definitionId, cancellationToken: cancellationToken);
            var build = new Build
            {
                Definition = pipeline,
                Project = pipeline.Project,
                SourceBranch = sourceBranch
            };
            var finishedBuild = await client
                .QueueBuildAsync(build, true, definitionId: pipeline.Id, cancellationToken: cancellationToken);
            return finishedBuild is not null ? AdapterStatus.Success : AdapterStatus.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running build pipeline {definitionId} for {project} project.", definitionId, project);
            return AdapterStatus.Unknown;
        }
    }

    public async Task<AdapterStatus> RunBuildPipelinesAsync(
        string organization,
        string pat,
        string project,
        IEnumerable<IDictionary<string, string>> pipelines,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            _logger.LogInformation("Request build pipelines from Azure DevOps.");
            _clientProvider.Setup(organization, pat);
            using var client = await _clientProvider.GetClientAsync<BuildHttpClient>(cancellationToken);
            var errorCounter = 0;
            foreach (var pipeline in pipelines)
            {
                var definitionId = int.Parse(pipeline["DefinitionId"]);
                var sourceBranch = pipeline["SourceBranch"];
                var receivedPipeline = await client.GetDefinitionAsync(project, definitionId, cancellationToken: cancellationToken);
                var build = new Build
                {
                    Definition = receivedPipeline,
                    Project = receivedPipeline.Project,
                    SourceBranch = sourceBranch
                };
                var finishedBuild = await client
                    .QueueBuildAsync(build, true, definitionId: receivedPipeline.Id, cancellationToken: cancellationToken);
                if (finishedBuild is null)
                {
                    errorCounter++;
                }
            }

            return errorCounter == 0 ? AdapterStatus.Success : AdapterStatus.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running build pipelines for {project} project.", project);
            return AdapterStatus.Unknown;
        }
    }
}
