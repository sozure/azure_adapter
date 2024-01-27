namespace VGManager.Adapter.Models.Requests;

public class GitBaseRequest<T> : ExtendedBaseRequest
{
    public T RepositoryId { get; set; } = default!;
}
