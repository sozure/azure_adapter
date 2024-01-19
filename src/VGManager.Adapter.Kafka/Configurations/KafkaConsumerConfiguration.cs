using Confluent.Kafka;

namespace VGManager.Adapter.Kafka.Configurations;

public class KafkaConsumerConfiguration<TMessageType>
{
    public ConsumerConfig ConsumerConfig { get; set; } = null!;
    public string Topic { get; set; } = null!;
    public string GroupIdPrefix { get; set; } = null!;
    public string SaslKerberosKeytabBase64 { get; set; } = null!;
    public int ConsumeTimeoutMS { get; set; } = 4000;
    public string MessageType => typeof(TMessageType).Name;
}
