using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Azure.Services.Requests;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;
public class ProjectAdapter : IProjectAdapter
{
    private ProjectHttpClient? _projectHttpClient;
    private readonly ILogger _logger;

    public ProjectAdapter(ILogger<ProjectAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<AdapterResponseModel<IEnumerable<ProjectRequest>>> GetProjectsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        BaseRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BaseRequest>(command.Payload);

            if (payload is null)
            {
                return new AdapterResponseModel<IEnumerable<ProjectRequest>>()
                {
                    Status = AdapterStatus.Unknown,
                    Data = Enumerable.Empty<ProjectRequest>()
                };
            }

            var baseUrl = $"https://dev.azure.com/{payload.Organization}";
            _logger.LogInformation("Get projects from {baseUrl}.", baseUrl);
            await GetConnectionAsync(baseUrl, payload.PAT);
            var teamProjectReferences = await _projectHttpClient!.GetProjects();
            _projectHttpClient.Dispose();

            var projects = new List<ProjectRequest>();

            foreach (var project in teamProjectReferences)
            {
                var subscriptionIds = await GetSubscriptionIds(baseUrl, project.Name, payload.PAT, cancellationToken);
                projects.Add(new()
                {
                    Project = project,
                    SubscriptionIds = subscriptionIds
                });
            }

            return GetResult(AdapterStatus.Success, projects);
        }
        catch (VssUnauthorizedException ex)
        {
            var status = AdapterStatus.Unauthorized;
            _logger.LogError(ex, "Couldn't get projects. Status: {status}.", status);
            return GetResult(status);
        }
        catch (VssServiceResponseException ex)
        {
            var status = AdapterStatus.ResourceNotFound;
            _logger.LogError(ex, "Couldn't get projects. Status: {status}.", status);
            return GetResult(status);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get projects. Status: {status}.", status);
            return GetResult(status);
        }
    }

    private async Task<IEnumerable<string>> GetSubscriptionIds(
        string baseUrl,
        string projectName,
        string personalAccessToken,
        CancellationToken cancellationToken
        )
    {
        var result = new List<string>();
        using var client = new HttpClient();
        // Set the base URL for Azure DevOps REST API
        client.BaseAddress = new Uri($"{baseUrl}/{projectName}/_apis/");

        // Set authorization header with the personal access token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"))
            );

        // Make a request to get the list of service connections
        var response = await client.GetAsync("serviceendpoint/endpoints?api-version=6.0", cancellationToken: cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            // Read and display the response
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var json = JsonSerializer.Deserialize<JsonObject>(responseBody) ?? [];
            var jsonArray = json["value"]?.AsArray() ?? [];
            foreach (var item in jsonArray)
            {
                var subscriptionId = item?["data"]?["subscriptionId"] ?? string.Empty;
                result.Add(subscriptionId.ToString());
            }
        }
        else
        {
            _logger.LogError("Error: {statusCode} - {reasonPhrase}", response.StatusCode, response.ReasonPhrase);
        }
        return result;
    }

    private async Task GetConnectionAsync(string baseUrl, string pat)
    {
        Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri);
        try
        {
            var credentials = new VssBasicCredential(string.Empty, pat);
            var connection = new VssConnection(uri, credentials);
            await connection.ConnectAsync(VssConnectMode.Profile, default);
            _projectHttpClient = new ProjectHttpClient(uri, credentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't establish connection.");
            throw;
        }
    }

    private static AdapterResponseModel<IEnumerable<ProjectRequest>> GetResult(AdapterStatus status, IEnumerable<ProjectRequest> projects)
    {
        return new()
        {
            Status = status,
            Data = projects
        };
    }

    private static AdapterResponseModel<IEnumerable<ProjectRequest>> GetResult(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = Enumerable.Empty<ProjectRequest>()
        };
    }
}
