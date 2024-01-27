using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure.Services.Helper;

public class HttpClientProvider : IHttpClientProvider
{
    private VssConnection _connection = null!;
    private ClientSecretCredential _secretCredential = null!;
    private Uri _secretUri = null!;

    public void Setup(string organization, string pat)
    {
        var uriString = $"https://dev.azure.com/{organization}";
        Uri uri;
        Uri.TryCreate(uriString, UriKind.Absolute, out uri!);

        var credentials = new VssBasicCredential(string.Empty, pat);
        _connection = new VssConnection(uri, credentials);
    }

    public void Setup(
        string keyVaultName,
        string tenantId,
        string clientId,
        string clientSecret
        )
    {
        _secretUri = new Uri($"https://{keyVaultName.ToLower()}.vault.azure.net/");
        _secretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    }

    public async Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default) where T : VssHttpClientBase
    {
        return await _connection.GetClientAsync<T>(cancellationToken);
    }

    public SecretClient GetSecretClient()
    {
        return new(_secretUri, _secretCredential);
    }
}
