using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure.Services.Helper;

public class HttpClientProvider : IHttpClientProvider
{
    private VssConnection _connection = null!;

    public void Setup(string organization, string pat)
    {
        var uriString = $"https://dev.azure.com/{organization}";
        Uri uri;
        Uri.TryCreate(uriString, UriKind.Absolute, out uri!);

        var credentials = new VssBasicCredential(string.Empty, pat);
        _connection = new VssConnection(uri, credentials);
    }

    public async Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default) where T : VssHttpClientBase
    {
        return await _connection.GetClientAsync<T>(cancellationToken);
    }
}
