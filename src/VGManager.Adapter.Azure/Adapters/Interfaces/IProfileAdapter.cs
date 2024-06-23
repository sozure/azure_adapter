using Microsoft.VisualStudio.Services.Profile;

namespace VGManager.Adapter.Azure.Adapters.Interfaces;

public interface IProfileAdapter
{
    Task<Profile?> GetProfileAsync(
        string organization,
        string pat,
        CancellationToken cancellationToken = default
        );
}
