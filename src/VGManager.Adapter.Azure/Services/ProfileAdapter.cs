using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.Profile.Client;
using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure.Services;

public class ProfileAdapter(IHttpClientProvider clientProvider, ILogger<ProfileAdapter> logger) : IProfileAdapter
{
    public async Task<Profile?> GetProfileAsync(
        string organization,
        string pat,
        CancellationToken cancellationToken = default
        )
    {
        try
        {
            logger.LogInformation("Request profile from Azure DevOps.");
            clientProvider.Setup(organization, pat);
            using var client = await clientProvider.GetClientAsync<ProfileHttpClient>(cancellationToken);
            var profileQueryContext = new ProfileQueryContext(AttributesScope.Core);
            return await client.GetProfileAsync(profileQueryContext, cancellationToken, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't get profile.");
            return null!;
        }
    }
}
