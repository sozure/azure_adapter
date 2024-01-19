using Confluent.Kafka;
using System.Text;
using System.Text.Json;
using VGManager.Adapter.Kafka.Interfaces;

namespace VGManager.Adapter.Kafka;

public class MessageSerializer<T> : IMessageSerializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            return default!;
        }

        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(data.ToArray()))!;
    }

    public byte[] Serialize(T data, SerializationContext context)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
    }
}
