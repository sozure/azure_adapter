using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using System.Text.RegularExpressions;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.StatusEnums;
namespace VGManager.Adapter.Azure.Services;

public class SprintAdapter(IHttpClientProvider clientProvider, ILogger<SprintAdapter> logger) : ISprintAdapter
{
    private readonly Regex _regex = new(@".*\b(\d+)\b.*", RegexOptions.Compiled);

    public async Task<(AdapterStatus, string)> GetCurrentSprintAsync(string project, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Request current sprint from {project} project.", project);
            using var workHttpClient = await clientProvider.GetClientAsync<WorkHttpClient>(cancellationToken);
            using var projectClient = await clientProvider.GetClientAsync<ProjectHttpClient>(cancellationToken);
            using var teamClient = await clientProvider.GetClientAsync<TeamHttpClient>(cancellationToken);

            var projects = await projectClient.GetProjects();
            var foundProject = projects.FirstOrDefault(x => x.Name == project);

            if (foundProject is null)
            {
                return (AdapterStatus.Unknown, string.Empty);
            }

            var teams = await teamClient.GetTeamsAsync(foundProject.Id.ToString(), cancellationToken: cancellationToken);
            var team = teams.FirstOrDefault();

            if (team is null)
            {
                return (AdapterStatus.Unknown, string.Empty);
            }

            var result = await workHttpClient.GetTeamSettingsAsync(new(project, team.Name), cancellationToken: cancellationToken);
            var sprintName = result.DefaultIteration.Name;

            if (!int.TryParse(_regex.Matches(sprintName)[0].Groups[1].Value, out var number))
            {
                return (AdapterStatus.Unknown, string.Empty);
            }

            return (AdapterStatus.Success, number.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting current sprint from {project} project.", project);
            return (AdapterStatus.Unknown, string.Empty);
        }
    }
}
