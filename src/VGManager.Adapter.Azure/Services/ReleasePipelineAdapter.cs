using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Gallery.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;
using System.Linq;
using Microsoft.TeamFoundation.Build.WebApi;

namespace VGManager.Adapter.Azure.Services;

public class ReleasePipelineAdapter(IHttpClientProvider clientProvider, ILogger<ReleasePipelineAdapter> logger) : IReleasePipelineAdapter
{
    private readonly string[] Replacable = { "Deploy to ", "Transfer to " };
    private readonly string[] ExcludableEnvironments = { "OTP container registry" };

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

            var project = payload.Project;
            var repositoryName = payload.RepositoryName;

            logger.LogInformation("Request environments for {repository} git repository from {project} azure project.", repositoryName, project);
            var definition = await GetReleaseDefinitionAsync(payload.Organization, payload.PAT, project, repositoryName, payload.ConfigFile, cancellationToken);
            var rawResult = definition?.Environments.Select(env => env.Name).ToList() ?? Enumerable.Empty<string>();
            var result = new List<string>();

            foreach (var rawElement in rawResult)
            {
                var element = Replacable.Where(rawElement.Contains).Select(replace => rawElement.Replace(replace, string.Empty));
                if (!element.Any())
                {
                    element = new[] { rawElement };
                }
                result.AddRange(element.Where(element => !ExcludableEnvironments.Contains(element)));
            }

            return ResponseProvider.GetResponse((
                definition is null ? AdapterStatus.Unknown : AdapterStatus.Success,
                result
                ));
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
        var environments = definition.Environments.Where(env => !ExcludableEnvironments.Any(env.Name.Contains));

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
        using var buildClient = await clientProvider.GetClientAsync<BuildHttpClient>(cancellationToken);
        var expand = ReleaseDefinitionExpands.Artifacts;
        var releaseDefinitions = await releaseClient.GetReleaseDefinitionsAsync(
            project,
            expand: expand,
            cancellationToken: cancellationToken
            );

        var foundDefinitions = new List<ReleaseDefinition>();

        foreach (var releaseDefinition in releaseDefinitions)
        {
            var artifacts = releaseDefinition.Artifacts.ToList();
            foreach (var artifact in artifacts)
            {
                var definitionId = artifact.DefinitionReference.GetValueOrDefault("definition")?.Id ?? string.Empty;

                var buildDef = await buildClient.GetDefinitionAsync(project, int.Parse(definitionId), cancellationToken: cancellationToken);
                if (buildDef.Name == repositoryName)
                {
                    foundDefinitions.Add(releaseDefinition);
                }
            }
        }

        ReleaseDefinition? definition = null!;

        foreach (var def in foundDefinitions)
        {
            var subResult = await releaseClient.GetReleaseDefinitionAsync(project, def?.Id ?? 0, cancellationToken: cancellationToken);

            var workFlowTasks = subResult?.Environments.FirstOrDefault()?.DeployPhases.FirstOrDefault()?.WorkflowTasks.ToList() ??
                Enumerable.Empty<WorkflowTask>();

            foreach (var task in workFlowTasks.Select(x => x.Inputs))
            {
                task.TryGetValue("configuration", out var configValue);
                task.TryGetValue("command", out var command);

                if ((configValue?.Contains(configFile) ?? false) && command == "apply")
                {
                    definition = subResult;
                }
            }
        }

        return definition;
    }
}
