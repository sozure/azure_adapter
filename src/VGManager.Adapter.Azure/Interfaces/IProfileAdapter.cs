using Microsoft.VisualStudio.Services.Profile;

namespace VGManager.Adapter.Azure.Interfaces;

public interface IProfileAdapter
{
    Task<Profile?> GetProfileAsync(string organization, string pat, CancellationToken cancellationToken = default);
}
