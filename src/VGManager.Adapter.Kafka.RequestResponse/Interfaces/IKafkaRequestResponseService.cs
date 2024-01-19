using VGManager.Adapter.Models.Interfaces;

namespace VGManager.Adapter.Kafka.RequestResponse.Interfaces;

public interface IKafkaRequestResponseService<TRequest, TResponse>
    where TRequest : ICommandRequest
    where TResponse : class
{
    Task<TResponse?> SendAndReceiveAsync(TRequest request, CancellationToken cancellationToken = default);
}
