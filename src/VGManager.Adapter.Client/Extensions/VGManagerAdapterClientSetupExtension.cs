using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VGManager.Adapter.Client.Configurations;
using VGManager.Adapter.Client.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Communication.Kafka.RequestResponse.Extensions;

namespace VGManager.Adapter.Client.Extensions;

public static class VGManagerAdapterClientSetupExtension
{
    private const string _kafkaProducerSectionKey = "VGManagerAdapterClientProducer";
    private const string _kafkaConsumerSectionKey = "VGManagerAdapterClientConsumer";
    private const string _clientConfigurationSectionKey = "VGManagerAdapterClientConfiguration";

    public static void SetupVGManagerAdapterClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(_ =>
            configuration
            .GetSection(_clientConfigurationSectionKey)
            .Get<VGManagerAdapterClientConfiguration>()
                ?? throw new InvalidOperationException("VGManagerAdapterClientConfiguration is missing from configuration.")
        );

        services.SetupKafkaRequestResponse<VGManagerAdapterCommand, VGManagerAdapterCommandResponse>(
            configuration,
            _kafkaProducerSectionKey,
            _kafkaConsumerSectionKey
        );

        services.AddScoped<IVGManagerAdapterClientService, VGManagerAdapterClientService>();
    }
}
