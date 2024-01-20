using Microsoft.VisualStudio.Services.WebApi;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IHttpClientProvider
{
    void Setup(string organization, string pat);

    Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default)
         where T : VssHttpClientBase;
}
