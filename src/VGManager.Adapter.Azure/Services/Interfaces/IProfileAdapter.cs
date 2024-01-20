using Microsoft.VisualStudio.Services.Profile;
using VGManager.Adapter.Models.Kafka;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IProfileAdapter
{
    Task<Profile?> GetProfileAsync(
        //string organization, 
        //string pat, 
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
