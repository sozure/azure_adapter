namespace VGManager.Adapter.Models.Response;

public record SimplifiedSecretResponse
{
    public string SecretName { get; set; } = null!;
    public string SecretValue { get; set; } = null!;
}
