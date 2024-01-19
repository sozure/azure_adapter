using Confluent.Kafka;

namespace VGManager.Adapter.Kafka.Configurations;

public class KafkaProducerConfiguration<TMessageType>
{
    public ProducerConfig ProducerConfig { get; set; } = null!;
    public string Topic { get; set; } = null!;
    public string SaslKerberosKeytabBase64 { get; set; } = null!;
    public string MessageType => typeof(TMessageType).Name;
}
