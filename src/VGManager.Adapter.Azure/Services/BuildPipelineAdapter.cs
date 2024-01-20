using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;
using System.Text.Json;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Azure.Services.Requests;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;
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

    public async Task<BaseResponse<IEnumerable<BuildDefinitionReference>>> GetBuildPipelinesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        ExtendedBaseRequest? payload;

        try
        {
            payload = JsonSerializer.Deserialize<ExtendedBaseRequest>(command.Payload);
        }
        catch (Exception)
        {
            return ResponseProvider.GetResponse(Enumerable.Empty<BuildDefinitionReference>());
        }

        if (payload is null)
        {
            return ResponseProvider.GetResponse(Enumerable.Empty<BuildDefinitionReference>());
        }

        _logger.LogInformation("Request build pipelines from Azure DevOps.");
        _clientProvider.Setup(payload.Organization, payload.PAT);
        using var client = await _clientProvider.GetClientAsync<BuildHttpClient>(cancellationToken);
        var result = await client.GetDefinitionsAsync(payload.Project, cancellationToken: cancellationToken);
        return ResponseProvider.GetResponse(result);
    }

    public async Task<BaseResponse<BuildDefinitionReference>> GetBuildPipelineAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        GetBuildPipelineRequest? payload;

        try
        {
            payload = JsonSerializer.Deserialize<GetBuildPipelineRequest>(command.Payload);
        }
        catch (Exception)
        {
            return null!;
        }

        if (payload is null)
        {
            return null!;
        }

        _logger.LogInformation("Request build pipelines from Azure DevOps.");
        _clientProvider.Setup(payload.Organization, payload.PAT);
        using var client = await _clientProvider.GetClientAsync<BuildHttpClient>(cancellationToken);
        var result = await client.GetDefinitionAsync(payload.Project, payload.Id, cancellationToken: cancellationToken);
        return ResponseProvider.GetResponse(result);
    }

    public async Task<BaseResponse<AdapterStatus>> RunBuildPipelineAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        RunBuildPipelineRequest? payload = null!;
        try
        {
            payload = JsonSerializer.Deserialize<RunBuildPipelineRequest>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse(AdapterStatus.Unknown);
            }
            _logger.LogInformation("Request build pipelines from Azure DevOps.");
            _clientProvider.Setup(payload.Organization, payload.PAT);
            using var client = await _clientProvider.GetClientAsync<BuildHttpClient>(cancellationToken);
            var pipeline = await client.GetDefinitionAsync(payload.Project, payload.Id, cancellationToken: cancellationToken);
            var build = new Build
            {
                Definition = pipeline,
                Project = pipeline.Project,
                SourceBranch = payload.SourceBranch
            };
            var finishedBuild = await client
                .QueueBuildAsync(build, true, definitionId: pipeline.Id, cancellationToken: cancellationToken);
            return ResponseProvider.GetResponse(finishedBuild is not null ? AdapterStatus.Success : AdapterStatus.Unknown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running build pipeline {definitionId} for {project} project.", payload?.Id ?? 0, payload?.Project ?? "Unknown");
            return ResponseProvider.GetResponse(AdapterStatus.Unknown);
        }
    }

    public async Task<BaseResponse<AdapterStatus>> RunBuildPipelinesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        RunBuildPipelinesRequest? payload = null!;
        try
        {
            payload = JsonSerializer.Deserialize<RunBuildPipelinesRequest>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse(AdapterStatus.Unknown);
            }

            _logger.LogInformation("Request build pipelines from Azure DevOps.");
            _clientProvider.Setup(payload.Organization, payload.PAT);
            using var client = await _clientProvider.GetClientAsync<BuildHttpClient>(cancellationToken);
            var errorCounter = 0;
            foreach (var pipeline in payload.Pipelines)
            {
                var definitionId = int.Parse(pipeline["DefinitionId"]);
                var sourceBranch = pipeline["SourceBranch"];
                var receivedPipeline = await client.GetDefinitionAsync(payload.Project, definitionId, cancellationToken: cancellationToken);
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

            return ResponseProvider.GetResponse(errorCounter == 0 ? AdapterStatus.Success : AdapterStatus.Unknown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running build pipelines for {project} project.", payload?.Project ?? "Unknown");
            return ResponseProvider.GetResponse(AdapterStatus.Unknown);
        }
    }
}
