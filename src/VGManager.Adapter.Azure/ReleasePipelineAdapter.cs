using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using VGManager.Adapter.Azure.Interfaces;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure;

public class ReleasePipelineAdapter : IReleasePipelineAdapter
{
    private readonly IHttpClientProvider _clientProvider;
    private readonly ILogger _logger;

    private readonly string[] Replacable = { "Deploy to ", "Transfer to " };
    private readonly string[] ExcludableEnvironments = { "OTP container registry" };

    public ReleasePipelineAdapter(IHttpClientProvider clientProvider, ILogger<ReleasePipelineAdapter> logger)
    {
        _clientProvider = clientProvider;
        _logger = logger;
    }

    public async Task<(AdapterStatus, IEnumerable<string>)> GetEnvironmentsAsync(
        string organization,
        string pat,
        string project,
        string repositoryName,
        string configFile,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            _logger.LogInformation("Request environments for {repository} git repository from {project} azure project.", repositoryName, project);
            var definition = await GetReleaseDefinitionAsync(organization, pat, project, repositoryName, configFile, cancellationToken);
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

            return (definition is null ? AdapterStatus.Unknown : AdapterStatus.Success, result);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            _logger.LogError(ex, "{project} azure project is not found.", project);
            return (AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting git branches from {project} azure project.", project);
            return (AdapterStatus.Unknown, Enumerable.Empty<string>());
        }
    }

    public async Task<(AdapterStatus, IEnumerable<(string, string)>)> GetVariableGroupsAsync(
        string organization,
        string pat,
        string project,
        string repositoryName,
        string configFile,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            _logger.LogInformation(
                "Request corresponding variable groups for {repository} repository, {project} azure project.",
                repositoryName,
                project
                );
            var definition = await GetReleaseDefinitionAsync(organization, pat, project, repositoryName, configFile, cancellationToken);

            if (definition is null)
            {
                return (AdapterStatus.Unknown, Enumerable.Empty<(string, string)>());
            }

            var variableGroups = await GetVariableGroupNames(project, definition, cancellationToken);

            return (AdapterStatus.Success, variableGroups);
        }
        catch (ProjectDoesNotExistWithNameException ex)
        {
            _logger.LogError(ex, "{project} azure project is not found.", project);
            return (AdapterStatus.ProjectDoesNotExist, Enumerable.Empty<(string, string)>());
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting corresponding variable groups for {repository} repository, {project} azure project.",
                repositoryName,
                project
                );
            return (AdapterStatus.Unknown, Enumerable.Empty<(string, string)>());
        }
    }

    private async Task<IEnumerable<(string, string)>> GetVariableGroupNames(
        string project,
        ReleaseDefinition definition,
        CancellationToken cancellationToken
        )
    {
        using var client = await _clientProvider.GetClientAsync<TaskAgentHttpClient>(cancellationToken: cancellationToken);
        var variableGroupNames = new List<(string, string)>();

        foreach (var env in definition.Environments.Where(env => !ExcludableEnvironments.Any(env.Name.Contains)))
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
        _clientProvider.Setup(organization, pat);
        using var client = await _clientProvider.GetClientAsync<ReleaseHttpClient>(cancellationToken);
        var expand = ReleaseDefinitionExpands.Artifacts;
        var releaseDefinitions = await client.GetReleaseDefinitionsAsync(
            project,
            expand: expand,
            cancellationToken: cancellationToken
            );

        var foundDefinitions = releaseDefinitions.Where(
            definition => definition.Artifacts.Any(artifact =>
            {
                var artifactType = artifact.DefinitionReference.GetValueOrDefault("definition")?.Name;
                return artifactType?.Equals(repositoryName) ?? false;
            })
            );

        ReleaseDefinition? definition = null!;

        foreach (var def in foundDefinitions)
        {
            var subResult = await client.GetReleaseDefinitionAsync(project, def?.Id ?? 0, cancellationToken: cancellationToken);

            var workFlowTasks = subResult?.Environments.FirstOrDefault()?.DeployPhases.FirstOrDefault()?.WorkflowTasks.ToList() ??
                Enumerable.Empty<WorkflowTask>();
            foreach (var task in workFlowTasks)
            {
                task.Inputs.TryGetValue("configuration", out var configValue);
                task.Inputs.TryGetValue("command", out var command);

                if ((configValue?.Contains(configFile) ?? false) && command == "apply")
                {
                    definition = subResult;
                }
            }
        }

        return definition;
    }
}
