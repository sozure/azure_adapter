using Azure.Security.KeyVault.Secrets;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IKeyVaultService
{
    Task<BaseResponse<Dictionary<string, object>>> GetKeyVaultsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterResponseModel<KeyVaultSecret?>>> GetSecretAsync(
        VGManagerAdapterCommand command,
        string name,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterStatus>> DeleteSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterResponseModel<IEnumerable<AdapterResponseModel<SimplifiedSecretResponse?>>>>> GetSecretsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterStatus>> AddKeyVaultSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterStatus>> RecoverSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    BaseResponse<AdapterResponseModel<IEnumerable<Dictionary<string, object>>>> GetDeletedSecrets(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterResponseModel<IEnumerable<KeyVaultSecret>>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
