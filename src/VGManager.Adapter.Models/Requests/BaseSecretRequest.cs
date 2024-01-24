namespace VGManager.Adapter.Models.Requests;

public class BaseSecretRequest
{
    public string TenantId { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string KeyVaultName { get; set; } = null!;
}
