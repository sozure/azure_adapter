using VGManager.Adapter.Kafka.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Services.Interfaces;

namespace VGManager.Adapter.Api.BackgroundServices;

public class CommandProcessorBackgroundService : BackgroundService
{
    private readonly IKafkaConsumerService<VGManagerAdapterCommand> _consumerService;
    private readonly IServiceProvider _serviceProvider;

    public CommandProcessorBackgroundService(
        IKafkaConsumerService<VGManagerAdapterCommand> consumerService,
        IServiceProvider serviceProvider
    )
    {
        _consumerService = consumerService;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumerService.ConsumeAsync(stoppingToken, async (message) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var commandProcessorService = scope.ServiceProvider.GetRequiredService<ICommandProcessorService>();

            await commandProcessorService.ProcessCommandAsync(message, stoppingToken);
        });
    }
}
