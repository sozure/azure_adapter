using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.Profile.Client;
using VGManager.Adapter.Azure.Interfaces;

namespace VGManager.Adapter.Azure;

public class ProfileAdapter : IProfileAdapter
{
    private readonly IHttpClientProvider _clientProvider;
    private readonly ILogger _logger;

    public ProfileAdapter(IHttpClientProvider clientProvider, ILogger<ProfileAdapter> logger)
    {
        _clientProvider = clientProvider;
        _logger = logger;
    }

    public async Task<Profile?> GetProfileAsync(string organization, string pat, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request profile from Azure DevOps.");
        _clientProvider.Setup(organization, pat);
        using var client = await _clientProvider.GetClientAsync<ProfileHttpClient>(cancellationToken);
        var profileQueryContext = new ProfileQueryContext(AttributesScope.Core);

        try
        {
            return await client.GetProfileAsync(profileQueryContext, cancellationToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't get profile.");
            return null!;
        }
    }
}
