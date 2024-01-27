using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.Profile.Client;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;

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

    public async Task<BaseResponse<Profile?>> GetProfileAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<BaseRequest>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                Profile? profile = null!;
                return ResponseProvider.GetResponse(profile);
            }

            _logger.LogInformation("Request profile from Azure DevOps.");
            _clientProvider.Setup(payload.Organization, payload.PAT);
            using var client = await _clientProvider.GetClientAsync<ProfileHttpClient>(cancellationToken);
            var profileQueryContext = new ProfileQueryContext(AttributesScope.Core);
            var result = await client.GetProfileAsync(profileQueryContext, cancellationToken, cancellationToken);
            return ResponseProvider.GetResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't get profile.");
            Profile? profile = null!;
            return ResponseProvider.GetResponse(profile);
        }
    }
}
