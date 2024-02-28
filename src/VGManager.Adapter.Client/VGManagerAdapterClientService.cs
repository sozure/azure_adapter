using Microsoft.Extensions.Logging;
using VGManager.Adapter.Client.Configurations;
using VGManager.Adapter.Client.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Communication.Kafka.Configurations;
using VGManager.Communication.Kafka.RequestResponse.Interfaces;

namespace VGManager.Adapter.Client;

public class VGManagerAdapterClientService(
    IKafkaRequestResponseService<VGManagerAdapterCommand, VGManagerAdapterCommandResponse> kafkaRequestResponseService,
    KafkaConsumerConfiguration<VGManagerAdapterCommandResponse> kafkaConsumerConfiguration,
    VGManagerAdapterClientConfiguration clientConfiguration,
    ILogger<VGManagerAdapterClientService> logger
    ) : IVGManagerAdapterClientService
{
    private const string VGManagerAdapterErrorMessage = "VGManagerAdapter did not respond in time.";

    public async Task<(bool isSuccess, string response)> SendAndReceiveMessageAsync(
        string commandType,
        string payload,
        CancellationToken cancellationToken
    )
    {
        (bool, string) result;

        try
        {
            var message = CreateMessage(commandType, payload);

            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(clientConfiguration.TimeoutMs);

            var response = await kafkaRequestResponseService.SendAndReceiveAsync(message, cancellationTokenSource.Token);

            result = (response?.IsSuccess ?? false, response?.Payload ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, VGManagerAdapterErrorMessage);
            return (false, string.Empty);
        }

        return result;
    }

    private VGManagerAdapterCommand CreateMessage(string commandType, string payload)
        => new()
        {
            InstanceId = Guid.NewGuid(),
            Destination = kafkaConsumerConfiguration.Topic,
            CommandSource = clientConfiguration.CommandSource,
            CommandType = commandType,
            Payload = payload,
            Timestamp = DateTime.UtcNow
        };
}
