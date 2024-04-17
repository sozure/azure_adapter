using Microsoft.VisualStudio.Services.Profile;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IProfileService
{
    Task<BaseResponse<Profile?>> GetProfileAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
