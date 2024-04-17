using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Profile;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;

namespace VGManager.Adapter.Azure.Services;

public class ProfileService(IProfileAdapter profileAdapter, ILogger<ProfileAdapter> logger) : IProfileService
{
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

            logger.LogInformation("Request profile from Azure DevOps.");
            var result = await profileAdapter.GetProfileAsync(payload.Organization, payload.PAT, cancellationToken);
            return ResponseProvider.GetResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't get profile.");
            Profile? profile = null!;
            return ResponseProvider.GetResponse(profile);
        }
    }
}
