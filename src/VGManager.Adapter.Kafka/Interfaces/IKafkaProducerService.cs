namespace VGManager.Adapter.Kafka.Interfaces;

public interface IKafkaProducerService<in TMessageType> : IDisposable
{
    Task ProduceAsync(TMessageType value, CancellationToken cancellationToken);
    Task ProduceAsync(TMessageType value, string topic, CancellationToken cancellationToken);
    Task ProduceAsync(TMessageType value, IEnumerable<string> topics, CancellationToken cancellationToken);
}
