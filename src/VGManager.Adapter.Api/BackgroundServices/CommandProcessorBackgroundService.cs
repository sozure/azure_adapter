using VGManager.Adapter.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Communication.Kafka.Interfaces;

namespace VGManager.Adapter.Api.BackgroundServices;

public class CommandProcessorBackgroundService(
    IKafkaConsumerService<VGManagerAdapterCommand> consumerService,
    IServiceProvider serviceProvider
    ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Consume");
        await consumerService.ConsumeAsync(async (message) =>
        {
            using var scope = serviceProvider.CreateScope();
            var commandProcessorService = scope.ServiceProvider.GetRequiredService<ICommandProcessorService>();

            await commandProcessorService.ProcessCommandAsync(message, stoppingToken);
        }, stoppingToken);
    }
}
