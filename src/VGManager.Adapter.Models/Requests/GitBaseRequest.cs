namespace VGManager.Adapter.Azure.Services.Requests;

public class GitBaseRequest<T> : ExtendedBaseRequest
{
    public T RepositoryId { get; set; } = default!;
}
