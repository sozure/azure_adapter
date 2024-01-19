using Confluent.Kafka;
using CorrelationId;
using CorrelationId.Abstractions;
using Microsoft.Extensions.Logging;
using VGManager.Adapter.Kafka.Configurations;
using VGManager.Adapter.Kafka.Interfaces;
using VGManager.Adapter.Models;

namespace VGManager.Adapter.Kafka;

public sealed class KafkaConsumerService<TMessageType> : IKafkaConsumerService<TMessageType> where TMessageType : MessageBase
{
    private readonly KafkaConsumerConfiguration<TMessageType> _consumerConfiguration;
    private readonly ILogger<KafkaConsumerService<TMessageType>> _logger;
    private readonly IConsumer<Ignore, TMessageType> _consumer;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public KafkaConsumerService(
        KafkaConsumerConfiguration<TMessageType> consumerConfiguration,
        ILogger<KafkaConsumerService<TMessageType>> logger,
        IConsumer<Ignore, TMessageType> consumer,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _consumerConfiguration = consumerConfiguration;
        _logger = logger;
        _consumer = consumer;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public async Task ConsumeAsync(CancellationToken cancellationToken, Func<TMessageType, Task> handlerMethod)
    {
        await ConsumeLogicAsync(handlerMethod, ProcessMessageParallelAsync, cancellationToken);
    }

    public async Task ConsumeSequentiallyAsync(Func<TMessageType, Task> handlerMethod, CancellationToken cancellationToken)
    {
        await ConsumeLogicAsync(handlerMethod, ProcessMessageSequentiallyAsync, cancellationToken);
    }

    private async Task ConsumeLogicAsync(
        Func<TMessageType, Task> handlerMethod,
        Func<Func<TMessageType, Task>, ConsumeResult<Ignore, TMessageType>, CancellationToken, Task> processMessageMethod,
        CancellationToken cancellationToken
    )
    {
        _consumer.Subscribe(_consumerConfiguration.Topic);

        await Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(cancellationToken);

                    if (consumeResult is not null)
                    {
                        var correlationId = consumeResult.Message?.Value?.CorrelationId ?? Guid.NewGuid().ToString();

                        using var loggerScope = _logger.BeginScope("{@CorrelationId}", correlationId);

                        LogMessage(consumeResult);

                        _correlationContextAccessor.CorrelationContext = new CorrelationContext(
                            correlationId,
                            CorrelationIdOptions.DefaultHeader
                        );

                        await processMessageMethod(
                            handlerMethod,
                            consumeResult,
                            cancellationToken
                        );
                    }
                }
                catch (ConsumeException e)
                {
                    _logger.LogError(
                        e,
                        "{MessageType} Consumer {MethodName} Error occured: {Reason}",
                        _consumerConfiguration.MessageType,
                        nameof(ConsumeLogicAsync),
                        e.Error.Reason
                    );
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        "{MessageType} Consumer {MethodName} Error occured.",
                        _consumerConfiguration.MessageType,
                        nameof(ConsumeLogicAsync)
                    );
                }
            }

            _logger.LogInformation("Cancellation requested. Unsubscribing consumer.");
            _consumer.Unsubscribe();
        }, cancellationToken);
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }

    private void LogMessage(ConsumeResult<Ignore, TMessageType> consumeResult)
    {
        try
        {
            _logger.LogInformation(
                "{MessageType} Consumer Consumed message at: '{TopicPartitionOffset} using consumer group {ConsumerGroup}'.",
                _consumerConfiguration.MessageType,
                consumeResult.TopicPartitionOffset,
                _consumerConfiguration.ConsumerConfig.GroupId
            );
            _logger.LogDebug(
                "{MessageType} Consumer Consumed message at: '{TopicPartitionOffset} using consumer group {ConsumerGroup}'. Message: {@Message}",
                _consumerConfiguration.MessageType,
                consumeResult.TopicPartitionOffset,
                _consumerConfiguration.ConsumerConfig.GroupId,
                consumeResult.Message?.Value
            );
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "{MessageType} Consumer {MethodName} Error occured.",
                _consumerConfiguration.MessageType,
                nameof(LogMessage)
            );
        }
    }

    private Task ProcessMessageParallelAsync(
        Func<TMessageType, Task> handlerMethod,
        ConsumeResult<Ignore, TMessageType> consumeResult,
        CancellationToken cancellationToken
    )
    {
        if (consumeResult.Message.Value is null)
        {
            _logger.LogWarning(
                "{MessageType} Consumer Received null message. This ConsumeResult instance represents an end of partition event. Skipping process.",
                _consumerConfiguration.MessageType
            );
        }
        else
        {
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await handlerMethod(consumeResult.Message.Value);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            e,
                            "{MessageType} Consumer {MethodName} Error occured.",
                            _consumerConfiguration.MessageType,
                            nameof(ProcessMessageParallelAsync)
                        );
                    }
                },
                cancellationToken
            );
        }

        return Task.CompletedTask;
    }

    private async Task ProcessMessageSequentiallyAsync(
        Func<TMessageType, Task> handlerMethod,
        ConsumeResult<Ignore, TMessageType> consumeResult,
        CancellationToken cancellationToken
    )
    {
        if (consumeResult.Message?.Value is null)
        {
            _logger.LogWarning(
                "{MessageType} Consumer Received null message. This ConsumeResult instance represents an end of partition event. Skipping process.",
                _consumerConfiguration.MessageType
            );
        }
        else
        {
            try
            {
                await handlerMethod(consumeResult.Message.Value);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "{MessageType} Consumer {MethodName} Error occured.",
                    _consumerConfiguration.MessageType,
                    nameof(ProcessMessageSequentiallyAsync)
                );
            }
        }
    }
}
