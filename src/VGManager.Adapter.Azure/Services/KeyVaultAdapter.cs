using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class KeyVaultAdapter : IKeyVaultAdapter
{
    private SecretClient _secretClient = null!;
    private readonly ILogger _logger;

    public KeyVaultAdapter(ILogger<KeyVaultAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<BaseResponse<Dictionary<string, object>>> GetKeyVaultsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        BaseSecretRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BaseSecretRequest>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse((string.Empty, Enumerable.Empty<string>()));
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;

            var result = new List<string>();
            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var client = new ArmClient(clientSecretCredential);
            var sub = await client.GetDefaultSubscriptionAsync(cancellationToken);
            var keyVaults = sub.GetKeyVaults(top: null, cancellationToken);

            foreach (var keyVault in keyVaults)
            {
                result.Add(keyVault.Data.Name);
            }

            return ResponseProvider.GetResponse((sub?.Id ?? string.Empty, result));
        }
        catch (Exception)
        {
            return ResponseProvider.GetResponse((string.Empty, Enumerable.Empty<string>()));
        }
    }

    public async Task<BaseResponse<AdapterResponseModel<KeyVaultSecret?>>> GetSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        KeyVaultSecret result;
        SecretRequest<string>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SecretRequest<string>>(command.Payload);

            if (payload is null)
            {
                var data = new AdapterResponseModel<KeyVaultSecret?>
                {
                    Data = null!,
                    Status = AdapterStatus.Unknown
                };

                return ResponseProvider.GetResponse(data);
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;

            Setup(payload.KeyVaultName, tenantId, clientId, clientSecret);
            result = await _secretClient.GetSecretAsync(payload.AdditionalData, cancellationToken: cancellationToken);
            return ResponseProvider.GetResponse(GetSecretResult(result));
        }
        catch (RequestFailedException ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get secret. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetSecretResult(status));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get secret. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetSecretResult(status));
        }
    }

    public async Task<BaseResponse<AdapterStatus>> DeleteSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        SecretRequest<string>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SecretRequest<string>>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse(AdapterStatus.Unknown);
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;
            var name = payload.AdditionalData;

            Setup(keyVaultName, tenantId, clientId, clientSecret);
            _logger.LogDebug("Delete secret {name} in {keyVault}.", name, keyVaultName);
            await _secretClient.StartDeleteSecretAsync(name, cancellationToken);
            return ResponseProvider.GetResponse(AdapterStatus.Success);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't delete secret. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
    }

    public async Task<BaseResponse<AdapterResponseModel<IEnumerable<AdapterResponseModel<KeyVaultSecret?>>>>> GetSecretsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        BaseSecretRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BaseSecretRequest>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse(new AdapterResponseModel<IEnumerable<AdapterResponseModel<KeyVaultSecret?>>>()
                {
                    Data = Enumerable.Empty<AdapterResponseModel<KeyVaultSecret?>>(),
                    Status = AdapterStatus.Unknown
                });
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;

            Setup(keyVaultName, tenantId, clientId, clientSecret);
            _logger.LogInformation("Get secrets from {keyVault}.", keyVaultName);
            var secretProperties = _secretClient.GetPropertiesOfSecrets(cancellationToken).ToList();
            var results = await Task.WhenAll(secretProperties.Select(p => GetSecretAsync(
                command
                )
            ));

            if (results is null)
            {
                return ResponseProvider.GetResponse(GetSecretsResult(AdapterStatus.Unknown));
            }

            return ResponseProvider.GetResponse(GetSecretsResult(results.Select(result => result.Data)));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get secrets. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetSecretsResult(status));
        }
    }

    public async Task<BaseResponse<AdapterStatus>> AddKeyVaultSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        SecretRequest<Dictionary<string, string>>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SecretRequest<Dictionary<string, string>>>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse(AdapterStatus.Unknown);
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;
            var parameters = payload.AdditionalData;

            Setup(keyVaultName, tenantId, clientId, clientSecret);
            _logger.LogInformation("Get deleted secrets from {keyVault}.", keyVaultName);
            var secretName = parameters["secretName"];
            var deletedSecrets = _secretClient.GetDeletedSecrets(cancellationToken).ToList();
            var didWeRecover = deletedSecrets.Exists(deletedSecret => deletedSecret.Name.Equals(secretName));

            if (!didWeRecover)
            {
                _logger.LogDebug("Set secret: {secretName} in {keyVault}.", secretName, keyVaultName);
                var secretValue = parameters["secretValue"];
                var newSecret = new KeyVaultSecret(secretName, secretValue);
                await _secretClient.SetSecretAsync(newSecret, cancellationToken);
            }
            else
            {
                _logger.LogDebug("Recover deleted secret: {secretName} in {keyVault}.", secretName, keyVaultName);
                await _secretClient.StartRecoverDeletedSecretAsync(secretName, cancellationToken);
            }

            return ResponseProvider.GetResponse(AdapterStatus.Success);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't add secret. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
    }

    public async Task<BaseResponse<AdapterStatus>> RecoverSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        SecretRequest<string>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SecretRequest<string>>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse(AdapterStatus.Unknown);
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;
            var name = payload.AdditionalData;

            Setup(keyVaultName, tenantId, clientId, clientSecret);
            _logger.LogDebug("Recover deleted secret: {secretName} in {keyVault}.", name, keyVaultName);
            await _secretClient.StartRecoverDeletedSecretAsync(name, cancellationToken);
            return ResponseProvider.GetResponse(AdapterStatus.Success);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't recover secret. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
    }

    public BaseResponse<AdapterResponseModel<IEnumerable<DeletedSecret>>> GetDeletedSecrets(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        BaseSecretRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BaseSecretRequest>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse(new AdapterResponseModel<IEnumerable<DeletedSecret>>()
                {
                    Data = Enumerable.Empty<DeletedSecret>(),
                    Status = AdapterStatus.Unknown
                });
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;

            Setup(keyVaultName, tenantId, clientId, clientSecret);
            _logger.LogInformation("Get deleted secrets from {keyVault}.", keyVaultName);
            var deletedSecrets = _secretClient.GetDeletedSecrets(cancellationToken).ToList();
            return ResponseProvider.GetResponse(GetDeletedSecretsResult(deletedSecrets));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get deleted secrets. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetDeletedSecretsResult(status));
        }
    }

    public async Task<BaseResponse<AdapterResponseModel<IEnumerable<KeyVaultSecret>>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        BaseSecretRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BaseSecretRequest>(command.Payload);

            if (payload is null)
            {
                return ResponseProvider.GetResponse(new AdapterResponseModel<IEnumerable<KeyVaultSecret>>()
                {
                    Data = Enumerable.Empty<DeletedSecret>(),
                    Status = AdapterStatus.Unknown
                });
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;

            Setup(keyVaultName, tenantId, clientId, clientSecret);
            var secretProperties = _secretClient.GetPropertiesOfSecrets(cancellationToken).ToList();
            var results = new List<KeyVaultSecret>();

            foreach (var secretProp in secretProperties)
            {
                var secret = await _secretClient.GetSecretAsync(secretProp.Name, cancellationToken: cancellationToken);
                if (secret is not null)
                {
                    results.Add(secret);
                }
            }

            return ResponseProvider.GetResponse(new AdapterResponseModel<IEnumerable<KeyVaultSecret>>()
            {
                Data = results,
                Status = AdapterStatus.Unknown
            });
        }
        catch (Exception)
        {
            return ResponseProvider.GetResponse(new AdapterResponseModel<IEnumerable<KeyVaultSecret>>()
            {
                Data = Enumerable.Empty<DeletedSecret>(),
                Status = AdapterStatus.Unknown
            });
        }
    }

    private void Setup(string keyVaultName, string tenantId, string clientId, string clientSecret)
    {
        var uri = new Uri($"https://{keyVaultName.ToLower()}.vault.azure.net/");
        var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _secretClient = new SecretClient(uri, clientSecretCredential);
    }

    private static AdapterResponseModel<IEnumerable<DeletedSecret>> GetDeletedSecretsResult(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = Enumerable.Empty<DeletedSecret>()
        };
    }

    private static AdapterResponseModel<IEnumerable<DeletedSecret>> GetDeletedSecretsResult(IEnumerable<DeletedSecret> deletedSecrets)
    {
        return new()
        {
            Status = AdapterStatus.Success,
            Data = deletedSecrets
        };
    }

    private static AdapterResponseModel<IEnumerable<AdapterResponseModel<KeyVaultSecret?>>> GetSecretsResult(
        IEnumerable<AdapterResponseModel<KeyVaultSecret?>> secrets
        )
    {
        return new()
        {
            Status = AdapterStatus.Success,
            Data = secrets
        };
    }

    private static AdapterResponseModel<IEnumerable<AdapterResponseModel<KeyVaultSecret?>>> GetSecretsResult(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = Enumerable.Empty<AdapterResponseModel<KeyVaultSecret?>>()
        };
    }

    private static AdapterResponseModel<KeyVaultSecret?> GetSecretResult(KeyVaultSecret result)
    {
        return new()
        {
            Status = AdapterStatus.Success,
            Data = result
        };
    }

    private static AdapterResponseModel<KeyVaultSecret?> GetSecretResult(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = null!
        };
    }
}
