using Microsoft.VisualStudio.Services.Profile;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IProfileAdapter
{
    Task<BaseResponse<Profile?>> GetProfileAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
