namespace VGManager.Adapter.Models.Requests;

public class SecretRequest<T> : BaseSecretRequest
{
    public T AdditionalData { get; set; } = default!;
}
