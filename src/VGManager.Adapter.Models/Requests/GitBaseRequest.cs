namespace VGManager.Adapter.Models.Requests;

public record GitBaseRequest<T> : ExtendedBaseRequest
{
    public T RepositoryId { get; set; } = default!;
}
