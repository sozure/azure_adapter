using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using VGManager.Adapter.Azure.Services.Helper;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class KeyVaultAdapter(
    IHttpClientProvider clientProvider,
    ILogger<KeyVaultAdapter> logger
        ) : IKeyVaultAdapter
{
    public async Task<BaseResponse<Dictionary<string, object>>> GetKeyVaultsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<BaseSecretRequest>.GetPayload(command.Payload);
        try
        {
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
        string name,
        CancellationToken cancellationToken = default
        )
    {
        KeyVaultSecret result;
        var payload = PayloadProvider<SecretRequest<string>>.GetPayload(command.Payload);
        try
        {
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

            clientProvider.Setup(payload.KeyVaultName, tenantId, clientId, clientSecret);
            var secretClient = clientProvider.GetSecretClient();
            result = await secretClient.GetSecretAsync(name, cancellationToken: cancellationToken);
            return ResponseProvider.GetResponse(GetSecretResult(result));
        }
        catch (RequestFailedException ex)
        {
            var status = AdapterStatus.Unknown;
            logger.LogError(ex, "Couldn't get secret. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetSecretResult(status));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            logger.LogError(ex, "Couldn't get secret. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetSecretResult(status));
        }
    }

    public async Task<BaseResponse<AdapterStatus>> DeleteSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        return await ChangeSecretAsync(command, "Delete", cancellationToken);
    }

    public async Task<BaseResponse<AdapterStatus>> RecoverSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        return await ChangeSecretAsync(command, "Recover", cancellationToken);
    }

    public async Task<BaseResponse<AdapterResponseModel<IEnumerable<AdapterResponseModel<SimplifiedSecretResponse?>>>>> GetSecretsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<SecretRequest<string>>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse(new AdapterResponseModel<IEnumerable<AdapterResponseModel<SimplifiedSecretResponse?>>>()
                {
                    Data = Enumerable.Empty<AdapterResponseModel<SimplifiedSecretResponse?>>(),
                    Status = AdapterStatus.Unknown
                });
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;

            logger.LogInformation("Get secrets from {keyVault}.", keyVaultName);
            clientProvider.Setup(payload.KeyVaultName, tenantId, clientId, clientSecret);
            var secretClient = clientProvider.GetSecretClient();
            var secretProperties = secretClient.GetPropertiesOfSecrets(cancellationToken).ToList();
            var results = await Task.WhenAll(secretProperties.Select(p => GetSecretAsync(
                command,
                p.Name
                )
            ));

            if (results is null)
            {
                return ResponseProvider.GetResponse(GetSecretsResult(AdapterStatus.Unknown));
            }

            var result = new List<AdapterResponseModel<SimplifiedSecretResponse?>>();

            foreach (var item in results)
            {
                var secret = item.Data.Data;
                if (secret is not null)
                {
                    result.Add(new()
                    {
                        Status = item.Data.Status,
                        Data = new SimplifiedSecretResponse
                        {
                            SecretName = secret.Name,
                            SecretValue = secret.Value
                        }
                    });
                }
            }

            return ResponseProvider.GetResponse(GetSecretsResult(result));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            logger.LogError(ex, "Couldn't get secrets. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetSecretsResult(status));
        }
    }

    public async Task<BaseResponse<AdapterStatus>> AddKeyVaultSecretAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<SecretRequest<Dictionary<string, string>>>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse(AdapterStatus.Unknown);
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;
            var parameters = payload.AdditionalData;

            logger.LogInformation("Get deleted secrets from {keyVault}.", keyVaultName);
            clientProvider.Setup(payload.KeyVaultName, tenantId, clientId, clientSecret);
            var secretClient = clientProvider.GetSecretClient();
            var secretName = parameters["secretName"];
            var deletedSecrets = secretClient.GetDeletedSecrets(cancellationToken).ToList();
            var didWeRecover = deletedSecrets.Exists(deletedSecret => deletedSecret.Name.Equals(secretName));

            if (!didWeRecover)
            {
                logger.LogDebug("Set secret: {secretName} in {keyVault}.", secretName, keyVaultName);
                var secretValue = parameters["secretValue"];
                var newSecret = new KeyVaultSecret(secretName, secretValue);
                await secretClient.SetSecretAsync(newSecret, cancellationToken);
            }
            else
            {
                logger.LogDebug("Recover deleted secret: {secretName} in {keyVault}.", secretName, keyVaultName);
                await secretClient.StartRecoverDeletedSecretAsync(secretName, cancellationToken);
            }

            return ResponseProvider.GetResponse(AdapterStatus.Success);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            logger.LogError(ex, "Couldn't add secret. Status: {status}.", status);
            return ResponseProvider.GetResponse(status);
        }
    }

    public BaseResponse<AdapterResponseModel<IEnumerable<Dictionary<string, object>>>> GetDeletedSecrets(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<BaseSecretRequest>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse(new AdapterResponseModel<IEnumerable<Dictionary<string, object>>>()
                {
                    Data = Enumerable.Empty<Dictionary<string, object>>(),
                    Status = AdapterStatus.Unknown
                });
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;

            logger.LogInformation("Get deleted secrets from {keyVault}.", keyVaultName);
            clientProvider.Setup(payload.KeyVaultName, tenantId, clientId, clientSecret);
            var secretClient = clientProvider.GetSecretClient();
            var deletedSecrets = secretClient.GetDeletedSecrets(cancellationToken).ToList();
            return ResponseProvider.GetResponse(GetDeletedSecretsResult(deletedSecrets));
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            logger.LogError(ex, "Couldn't get deleted secrets. Status: {status}.", status);
            return ResponseProvider.GetResponse(GetDeletedSecretsResult(status));
        }
    }

    public async Task<BaseResponse<AdapterResponseModel<IEnumerable<KeyVaultSecret>>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<BaseSecretRequest>.GetPayload(command.Payload);
        try
        {
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

            clientProvider.Setup(payload.KeyVaultName, tenantId, clientId, clientSecret);
            var secretClient = clientProvider.GetSecretClient();
            var secretProperties = secretClient.GetPropertiesOfSecrets(cancellationToken).ToList();
            var results = new List<KeyVaultSecret>();

            foreach (var secretProp in secretProperties)
            {
                var secret = await secretClient.GetSecretAsync(secretProp.Name, cancellationToken: cancellationToken);
                if (secret is not null)
                {
                    results.Add(secret);
                }
            }

            return ResponseProvider.GetResponse(new AdapterResponseModel<IEnumerable<KeyVaultSecret>>()
            {
                Data = results,
                Status = AdapterStatus.Success
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

    private async Task<BaseResponse<AdapterStatus>> ChangeSecretAsync(
        VGManagerAdapterCommand command,
        string actionType,
        CancellationToken cancellationToken = default
        )
    {
        var payload = PayloadProvider<SecretRequest<string>>.GetPayload(command.Payload);
        try
        {
            if (payload is null)
            {
                return ResponseProvider.GetResponse(AdapterStatus.Unknown);
            }

            var tenantId = payload.TenantId;
            var clientId = payload.ClientId;
            var clientSecret = payload.ClientSecret;
            var keyVaultName = payload.KeyVaultName;
            var name = payload.AdditionalData;

            logger.LogDebug("{actionType} secret: {secretName} in {keyVault}.", actionType, name, keyVaultName);
            clientProvider.Setup(payload.KeyVaultName, tenantId, clientId, clientSecret);
            var secretClient = clientProvider.GetSecretClient();
            if (actionType == "Recover")
            {
                await secretClient.StartRecoverDeletedSecretAsync(name, cancellationToken);
            } else
            {
                await secretClient.StartDeleteSecretAsync(name, cancellationToken);
            }
            return ResponseProvider.GetResponse(AdapterStatus.Success);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            logger.LogError(ex, "Couldn't {actionType} secret. Status: {status}.", actionType.ToLower(), status);
            return ResponseProvider.GetResponse(status);
        }
    }

    private static AdapterResponseModel<IEnumerable<Dictionary<string, object>>> GetDeletedSecretsResult(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = Enumerable.Empty<Dictionary<string, object>>()
        };
    }

    private static AdapterResponseModel<IEnumerable<Dictionary<string, object>>> GetDeletedSecretsResult(
        IEnumerable<DeletedSecret> deletedSecrets
        )
    {
        var res = new List<Dictionary<string, object>>();
        foreach (var deletedSecret in deletedSecrets)
        {
            res.Add(new()
            {
                ["Name"] = deletedSecret.Name,
                ["Value"] = deletedSecret.Value,
                ["DeletedOn"] = deletedSecret.DeletedOn!
            });
        }
        return new()
        {
            Status = AdapterStatus.Success,
            Data = res
        };
    }

    private static AdapterResponseModel<IEnumerable<AdapterResponseModel<SimplifiedSecretResponse?>>> GetSecretsResult(
        IEnumerable<AdapterResponseModel<SimplifiedSecretResponse?>> secrets
        )
    {
        return new()
        {
            Status = AdapterStatus.Success,
            Data = secrets
        };
    }

    private static AdapterResponseModel<IEnumerable<AdapterResponseModel<SimplifiedSecretResponse?>>> GetSecretsResult(AdapterStatus status)
    {
        return new()
        {
            Status = status,
            Data = Enumerable.Empty<AdapterResponseModel<SimplifiedSecretResponse?>>()
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
