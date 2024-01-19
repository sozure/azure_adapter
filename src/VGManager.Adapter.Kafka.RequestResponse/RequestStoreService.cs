using System.Collections.Concurrent;
using VGManager.Adapter.Kafka.RequestResponse.Interfaces;

namespace VGManager.Adapter.Kafka.RequestResponse;

public class RequestStoreService<TResponse> : IRequestStoreService<TResponse>
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<TResponse>> store = new ConcurrentDictionary<Guid, TaskCompletionSource<TResponse>>();

    public bool Add(Guid id, TaskCompletionSource<TResponse> task)
    {
        return store.TryAdd(id, task);
    }

    public TaskCompletionSource<TResponse>? GetAndRemove(Guid id)
    {
        if (store.TryRemove(id, out var result))
        {
            return result;
        }

        return null;
    }

    public bool Remove(Guid id)
    {
        return store.TryRemove(id, out _);
    }
}
