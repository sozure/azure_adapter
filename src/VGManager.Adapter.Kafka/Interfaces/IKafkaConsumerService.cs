namespace VGManager.Adapter.Kafka.Interfaces;

public interface IKafkaConsumerService<out TMessageType> : IDisposable
{
    Task ConsumeAsync(CancellationToken cancellationToken, Func<TMessageType, Task> handlerMethod);
    Task ConsumeSequentiallyAsync(Func<TMessageType, Task> handlerMethod, CancellationToken cancellationToken);
}
