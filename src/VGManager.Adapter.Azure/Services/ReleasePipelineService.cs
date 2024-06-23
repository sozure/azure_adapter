using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using VGManager.Adapter.Azure.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Azure.Settings;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class ReleasePipelineService(
    IHttpClientProvider clientProvider,
    IOptions<ReleasePipelineAdapterSettings> options,
    ILogger<ReleasePipelineService> logger
    ) : IReleasePipelineService
{
    private readonly ReleasePipelineAdapterSettings Settings = options.Value;

    public async Task<BaseResponse<Dictionary<string, object>>> GetEnvironmentsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<ReleasePipelineRequest>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
            }

            (var definition, var result) = await GetEnvironmentsAsync(
                payload.Organization,
                payload.PAT,
                payload.Project,
                payload.RepositoryName,
                payload.ConfigFile,
                cancellationToken
                );

            return ResponseProvider.GetResponse((definition is null ? AdapterStatus.Unknown : AdapterStatus.Success, result));
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            logger.LogError(ex, "{project} azure project is not found.", payload?.Project ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting git branches from {project} azure project.", payload?.Project ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
        }
    }

    public async Task<BaseResponse<Dictionary<string, object>>> GetEnvironmentsFromMultipleProjectsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<MultipleReleasePipelineRequest>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
            }

            var result = new List<string>();

            foreach (var project in payload.Projects)
            {
                (_, var subResult) = await GetEnvironmentsAsync(
                    payload.Organization,
                    payload.PAT, 
                    project,
                    payload.RepositoryName,
                    payload.ConfigFile,
                    cancellationToken
                    );

                if (subResult.Any())
                {
                    result.Add(project);
                }
            }

            return ResponseProvider.GetResponse((AdapterStatus.Success, result));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, 
                "Error getting environments from azure projecs in {organization} organization.", 
                payload?.Organization ?? "Unknown"
                );

            return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<string>()));
        }
    }

    public async Task<BaseResponse<Dictionary<string, object>>> GetVariableGroupsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<ReleasePipelineRequest>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<(string, string)>()));
            }

            var project = payload.Project;
            var repositoryName = payload.RepositoryName;

            logger.LogInformation(
                "Request corresponding variable groups for {repository} repository, {project} azure project.",
                repositoryName,
                project
                );
            var definition = await GetReleaseDefinitionAsync(payload.Organization, payload.PAT, project, repositoryName, payload.ConfigFile, cancellationToken);

            if (definition is null)
            {
                return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<(string, string)>()));
            }

            var variableGroups = await GetVariableGroupNames(project, definition, cancellationToken);

            return ResponseProvider.GetResponse((AdapterStatus.Success, variableGroups));
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            logger.LogError(ex, "{project} azure project is not found.", payload?.Project ?? "Unknown");
            return ResponseProvider.GetResponse((AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<(string, string)>()));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error getting corresponding variable groups for {repository} repository, {project} azure project.",
                payload?.RepositoryName ?? "Unknown",
                payload?.Project ?? "Unknown"
                );
            return ResponseProvider.GetResponse((AdapterStatus.Unknown, Enumerable.Empty<(string, string)>()));
        }
    }

    private async Task<IEnumerable<(string, string)>> GetVariableGroupNames(
        string project,
        ReleaseDefinition definition,
        CancellationToken cancellationToken
        )
    {
        using var client = await clientProvider.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
        var variableGroupNames = new List<(string, string)>();
        var environments = definition.Environments.Where(env => !Settings.ExcludableEnvironments.Any(env.Name.Contains));

        foreach (var env in environments)
        {
            foreach (var id in env.VariableGroups)
            {
                var vg = await client.GetVariableGroupAsync(project, id, cancellationToken: cancellationToken);
                variableGroupNames.Add((vg.Name, vg.Type));
            }
        }

        return variableGroupNames;
    }

    private async Task<ReleaseDefinition?> GetReleaseDefinitionAsync(
        string organization,
        string pat,
        string project,
        string repositoryName,
        string configFile,
        CancellationToken cancellationToken
        )
    {
        clientProvider.Setup(organization, pat);
        using var releaseClient = await clientProvider.GetClientAsync<ReleaseHttpClient>(cancellationToken);
        var expand = ReleaseDefinitionExpands.Artifacts;
        var releaseDefinitions = await releaseClient.GetReleaseDefinitionsAsync(project, expand: expand, cancellationToken: cancellationToken);

        var filteredReleaseDefinitions = releaseDefinitions.Where(
                releaseDef => releaseDef.Artifacts.Any(
                    artifact => artifact.DefinitionReference.GetValueOrDefault("definition")?.Name == repositoryName
                    )
            );

        foreach(var releaseDef in filteredReleaseDefinitions)
        {
            var detailedReleaseDef = await releaseClient.GetReleaseDefinitionAsync(
                project,
                definitionId: releaseDef.Id,
                cancellationToken: cancellationToken
                );

            var workFlowTasks = detailedReleaseDef?.Environments.FirstOrDefault()?.DeployPhases.FirstOrDefault()?.WorkflowTasks.ToList() ??
                Enumerable.Empty<WorkflowTask>();

            var filteredWorkFlowTasks = workFlowTasks.Select(x => x.Inputs);
            foreach (var task in filteredWorkFlowTasks)
            {
                task.TryGetValue("configuration", out var configValue);
                task.TryGetValue("command", out var command);

                if ((configValue?.Contains(configFile) ?? false) && command == "apply")
                {
                    return detailedReleaseDef;
                }
            }
        }

        return null!;
    }

    private async Task<(ReleaseDefinition?, IEnumerable<string>)> GetEnvironmentsAsync(
        string organization,
        string pat,
        string project,
        string repositoryName,
        string configFile,
        CancellationToken cancellationToken
        )
    {
        logger.LogInformation("Request environments for {repository} git repository from {project} azure project.", repositoryName, project);
        var definition = await GetReleaseDefinitionAsync(organization, pat, project, repositoryName, configFile, cancellationToken);

        var rawResult = definition?.Environments.Select(env => env.Name).ToList() ?? Enumerable.Empty<string>();
        var result = new List<string>();

        foreach (var rawElement in rawResult)
        {
            var element = Settings.Replacable.Where(rawElement.Contains).Select(replace => rawElement.Replace(replace, string.Empty));
            if (!element.Any())
            {
                element = [rawElement];
            }
            result.AddRange(element.Where(element => !Settings.ExcludableEnvironments.Contains(element)));
        }

        return (definition, result);
    }
}
