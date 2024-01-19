using Confluent.Kafka;
using CorrelationId;
using CorrelationId.Abstractions;
using Microsoft.Extensions.Logging;
using VGManager.Adapter.Kafka.Configurations;
using VGManager.Adapter.Kafka.Interfaces;
using VGManager.Adapter.Models;

namespace VGManager.Adapter.Kafka;

public sealed class KafkaProducerService<TMessageType> : IKafkaProducerService<TMessageType> where TMessageType : MessageBase
{
    private readonly KafkaProducerConfiguration<TMessageType> _producerConfiguration;
    private readonly ILogger<KafkaProducerService<TMessageType>> _logger;
    private readonly IProducer<Null, TMessageType> _producer;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public KafkaProducerService(
        KafkaProducerConfiguration<TMessageType> producerConfiguration,
        IProducer<Null, TMessageType> producer,
        ILogger<KafkaProducerService<TMessageType>> logger,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _producerConfiguration = producerConfiguration;
        _logger = logger;
        _producer = producer;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public async Task ProduceAsync(TMessageType value, CancellationToken cancellationToken)
    {
        await ProduceAsync(value, _producerConfiguration.Topic, cancellationToken);
    }

    public async Task ProduceAsync(TMessageType value, string topic, CancellationToken cancellationToken)
    {
        var message = new Message<Null, TMessageType>
        {
            Value = value
        };

        await ProduceAsync(message, topic, cancellationToken);
    }

    public async Task ProduceAsync(TMessageType value, IEnumerable<string> topics, CancellationToken cancellationToken)
    {
        var message = new Message<Null, TMessageType>
        {
            Value = value
        };

        var producerTasks = new List<Task>();

        foreach (var topic in topics)
        {
            producerTasks.Add(ProduceAsync(message, topic, cancellationToken));
        }

        await Task.WhenAll(producerTasks);
    }

    private async Task ProduceAsync(Message<Null, TMessageType> message, string topic, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = _correlationContextAccessor.CorrelationContext?.CorrelationId ?? Guid.NewGuid().ToString();

            _correlationContextAccessor.CorrelationContext = new CorrelationContext(correlationId, CorrelationIdOptions.DefaultHeader);

            message.Value.CorrelationId = correlationId;

            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation($"{nameof(KafkaProducerService<TMessageType>)}.{nameof(ProduceAsync)} Delivered message to topic: '{deliveryResult.Topic}' with offset: '{deliveryResult.TopicPartitionOffset}'");
            _logger.LogDebug("{@Message}", message.Value);
        }
        catch (ProduceException<Null, TMessageType> e)
        {
            _logger.LogError(e, $"{nameof(KafkaProducerService<TMessageType>)}.{nameof(ProduceAsync)} Delivery failed: {e.Error.Reason}");
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
