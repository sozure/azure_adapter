using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VGManager.Adapter.Kafka.Configurations;
using VGManager.Adapter.Kafka.Interfaces;
using VGManager.Adapter.Messaging.Models;

namespace VGManager.Adapter.Kafka.Extensions;

public static class KafkaProducerSetupExtension
{
    private const string LogMessageTemplate = "{@KafkaLogMessage}";

    public static void SetupKafkaProducer<TMessageType>(
        this IServiceCollection services,
        IConfiguration configuration,
        string kafkaProducerSectionKey)
        where TMessageType : MessageBase
    {
        var producerConfig = configuration.GetSection(kafkaProducerSectionKey)
           .Get<KafkaProducerConfiguration<TMessageType>>();

        services.AddSingleton(serviceProvider =>
        {
            return new ProducerBuilder<Null, TMessageType>(producerConfig.ProducerConfig)
            .SetValueSerializer(new MessageSerializer<TMessageType>())
            .SetLogHandler(LogHandler<TMessageType>(serviceProvider))
            .Build();
        });

        services.AddSingleton(producerConfig);
        services.AddSingleton<IKafkaProducerService<TMessageType>, KafkaProducerService<TMessageType>>();
    }

    private static Action<IProducer<Null, TMessageType>, LogMessage> LogHandler<TMessageType>(IServiceProvider serviceProvider) where TMessageType : MessageBase
    {
        return (producer, logMessage) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<KafkaProducerService<TMessageType>>>();

            switch (logMessage.Level)
            {
                case SyslogLevel.Emergency:
                case SyslogLevel.Alert:
                case SyslogLevel.Critical:
                    logger.LogCritical(LogMessageTemplate, logMessage);
                    break;
                case SyslogLevel.Error:
                    logger.LogError(LogMessageTemplate, logMessage);
                    break;
                case SyslogLevel.Warning:
                    logger.LogWarning(LogMessageTemplate, logMessage);
                    break;
                case SyslogLevel.Notice:
                case SyslogLevel.Info:
                    logger.LogInformation(LogMessageTemplate, logMessage);
                    break;
                case SyslogLevel.Debug:
                    logger.LogDebug(LogMessageTemplate, logMessage);
                    break;
            }
        };
    }
}
