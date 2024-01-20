using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.Profile.Client;
using System.Text.Json;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Azure.Services.Requests;
using VGManager.Adapter.Models.Kafka;

namespace VGManager.Adapter.Azure.Services;

public class ProfileAdapter : IProfileAdapter
{
    private readonly IHttpClientProvider _clientProvider;
    private readonly ILogger _logger;

    public ProfileAdapter(IHttpClientProvider clientProvider, ILogger<ProfileAdapter> logger)
    {
        _clientProvider = clientProvider;
        _logger = logger;
    }

    public async Task<Profile?> GetProfileAsync(
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
                return null;
            }

            _logger.LogInformation("Request profile from Azure DevOps.");
            _clientProvider.Setup(payload.Organization, payload.PAT);
            using var client = await _clientProvider.GetClientAsync<ProfileHttpClient>(cancellationToken);
            var profileQueryContext = new ProfileQueryContext(AttributesScope.Core);
            return await client.GetProfileAsync(profileQueryContext, cancellationToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't get profile.");
            return null;
        }
    }
}
