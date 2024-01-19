namespace VGManager.Adapter.Kafka.RequestResponse.Interfaces;

public interface IRequestStoreService<TResponse>
{
    bool Add(Guid id, TaskCompletionSource<TResponse> task);

    TaskCompletionSource<TResponse>? GetAndRemove(Guid id);

    bool Remove(Guid id);
}
