using Confluent.Kafka;

namespace VGManager.Adapter.Kafka.Interfaces;

public interface IMessageSerializer<T> : IDeserializer<T>, ISerializer<T>
{
}
