using Azure.Security.KeyVault.Secrets;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Interfaces;

public interface IKeyVaultAdapter
{
    Task<(string?, IEnumerable<string>)> GetKeyVaultsAsync(
        string tenantId,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default
        );
    Task<AdapterStatus> AddKeyVaultSecretAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default);
    Task<AdapterStatus> DeleteSecretAsync(string name, CancellationToken cancellationToken = default);
    Task<AdapterResponseModel<KeyVaultSecret?>> GetSecretAsync(string name, CancellationToken cancellationToken = default);
    Task<AdapterResponseModel<IEnumerable<AdapterResponseModel<KeyVaultSecret?>>>> GetSecretsAsync(CancellationToken cancellationToken = default);
    Task<AdapterStatus> RecoverSecretAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<KeyVaultSecret>> GetAllAsync(CancellationToken cancellationToken);
    AdapterResponseModel<IEnumerable<DeletedSecret>> GetDeletedSecrets(CancellationToken cancellationToken = default);
    public void Setup(string keyVaultName, string tenantId, string clientId, string clientSecret);
}
