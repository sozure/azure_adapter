using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services;

public class KeyVaultAdapter : IKeyVaultAdapter
{
    private SecretClient _secretClient = null!;
    private readonly ILogger _logger;
    private string _keyVaultName = null!;

    public KeyVaultAdapter(ILogger<KeyVaultAdapter> logger)
    {
        _logger = logger;
    }

    public void Setup(string keyVaultName, string tenantId, string clientId, string clientSecret)
    {
        var uri = new Uri($"https://{keyVaultName.ToLower()}.vault.azure.net/");
        var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        _secretClient = new SecretClient(uri, clientSecretCredential);
        _keyVaultName = keyVaultName;
    }

    public async Task<(string?, IEnumerable<string>)> GetKeyVaultsAsync(
        string tenantId,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default
        )
    {
        var result = new List<string>();
        var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var client = new ArmClient(clientSecretCredential);
        var sub = await client.GetDefaultSubscriptionAsync(cancellationToken);
        var keyVaults = sub.GetKeyVaults(top: null, cancellationToken);

        foreach (var keyVault in keyVaults)
        {
            result.Add(keyVault.Data.Name);
        }

        return (sub?.Id ?? string.Empty, result);
    }

    public async Task<AdapterResponseModel<KeyVaultSecret?>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        KeyVaultSecret result;
        try
        {
            result = await _secretClient.GetSecretAsync(name, cancellationToken: cancellationToken);
            return GetSecretResult(result);
        }
        catch (RequestFailedException ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get secret. Status: {status}.", status);
            return GetSecretResult(status);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get secret. Status: {status}.", status);
            return GetSecretResult(status);
        }
    }

    public async Task<AdapterStatus> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Delete secret {name} in {keyVault}.", name, _keyVaultName);
            await _secretClient.StartDeleteSecretAsync(name, cancellationToken);
            return AdapterStatus.Success;
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't delete secret. Status: {status}.", status);
            return status;
        }
    }

    public async Task<AdapterResponseModel<IEnumerable<AdapterResponseModel<KeyVaultSecret?>>>> GetSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Get secrets from {keyVault}.", _keyVaultName);
            var secretProperties = _secretClient.GetPropertiesOfSecrets(cancellationToken).ToList();
            var results = await Task.WhenAll(secretProperties.Select(p => GetSecretAsync(p.Name)));

            if (results is null)
            {
                return GetSecretsResult(AdapterStatus.Unknown);
            }
            return GetSecretsResult(results.ToList());

        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get secrets. Status: {status}.", status);
            return GetSecretsResult(status);
        }
    }

    public async Task<AdapterStatus> AddKeyVaultSecretAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Get deleted secrets from {keyVault}.", _keyVaultName);
            var secretName = parameters["secretName"];
            var deletedSecrets = _secretClient.GetDeletedSecrets(cancellationToken).ToList();
            var didWeRecover = deletedSecrets.Exists(deletedSecret => deletedSecret.Name.Equals(secretName));

            if (!didWeRecover)
            {
                _logger.LogDebug("Set secret: {secretName} in {keyVault}.", secretName, _keyVaultName);
                var secretValue = parameters["secretValue"];
                var newSecret = new KeyVaultSecret(secretName, secretValue);
                await _secretClient.SetSecretAsync(newSecret, cancellationToken);
            }
            else
            {
                _logger.LogDebug("Recover deleted secret: {secretName} in {keyVault}.", secretName, _keyVaultName);
                await _secretClient.StartRecoverDeletedSecretAsync(secretName, cancellationToken);
            }

            return AdapterStatus.Success;
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't add secret. Status: {status}.", status);
            return status;
        }
    }

    public async Task<AdapterStatus> RecoverSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Recover deleted secret: {secretName} in {keyVault}.", name, _keyVaultName);
            await _secretClient.StartRecoverDeletedSecretAsync(name, cancellationToken);
            return AdapterStatus.Success;
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't recover secret. Status: {status}.", status);
            return status;
        }
    }

    public AdapterResponseModel<IEnumerable<DeletedSecret>> GetDeletedSecrets(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Get deleted secrets from {keyVault}.", _keyVaultName);
            var deletedSecrets = _secretClient.GetDeletedSecrets(cancellationToken).ToList();
            return GetDeletedSecretsResult(deletedSecrets);
        }
        catch (Exception ex)
        {
            var status = AdapterStatus.Unknown;
            _logger.LogError(ex, "Couldn't get deleted secrets. Status: {status}.", status);
            return GetDeletedSecretsResult(status);
        }
    }

    public async Task<IEnumerable<KeyVaultSecret>> GetAllAsync(CancellationToken cancellationToken)
    {
        var secretProperties = _secretClient.GetPropertiesOfSecrets(cancellationToken).ToList();
        var results = new List<KeyVaultSecret>();

        foreach (var secretProp in secretProperties)
        {
            var secret = await _secretClient.GetSecretAsync(secretProp.Name, cancellationToken: cancellationToken);
            results.Add(secret);
        }

        return results;
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
