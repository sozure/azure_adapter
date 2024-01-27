using Azure.Security.KeyVault.Secrets;
using Microsoft.VisualStudio.Services.WebApi;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IHttpClientProvider
{
    void Setup(string organization, string pat);

    void Setup(
        string keyVaultName,
        string tenantId,
        string clientId,
        string clientSecret
        );

    Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default)
         where T : VssHttpClientBase;

    SecretClient GetSecretClient();
}
