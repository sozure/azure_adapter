namespace VGManager.Adapter.Models.Requests;

public record SecretRequest<T> : BaseSecretRequest
{
    public T AdditionalData { get; set; } = default!;
}
