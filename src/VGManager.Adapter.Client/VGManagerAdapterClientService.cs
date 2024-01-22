using Microsoft.Extensions.Logging;
using VGManager.Adapter.Client.Configurations;
using VGManager.Adapter.Client.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Communication.Kafka.Configurations;
using VGManager.Communication.Kafka.RequestResponse.Interfaces;

namespace VGManager.Adapter.Client;

public class VGManagerAdapterClientService : IVGManagerAdapterClientService
{
    private const string VGManagerAdapterErrorMessage = "VGManagerAdapter did not respond in time.";

    private readonly IKafkaRequestResponseService<VGManagerAdapterCommand, VGManagerAdapterCommandResponse> _kafkaRequestResponseService;
    private readonly KafkaConsumerConfiguration<VGManagerAdapterCommandResponse> _kafkaConsumerConfiguration;
    private readonly VGManagerAdapterClientConfiguration _clientConfiguration;

    private readonly ILogger _logger;

    public VGManagerAdapterClientService(
        IKafkaRequestResponseService<VGManagerAdapterCommand, VGManagerAdapterCommandResponse> kafkaRequestResponseService,
        KafkaConsumerConfiguration<VGManagerAdapterCommandResponse> kafkaConsumerConfiguration,
        VGManagerAdapterClientConfiguration clientConfiguration,
        ILogger<VGManagerAdapterClientService> logger
    )
    {
        _kafkaRequestResponseService = kafkaRequestResponseService;
        _kafkaConsumerConfiguration = kafkaConsumerConfiguration;
        _clientConfiguration = clientConfiguration;
        _logger = logger;
    }

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
            cancellationTokenSource.CancelAfter(_clientConfiguration.TimeoutMs);

            var response = await _kafkaRequestResponseService.SendAndReceiveAsync(message, cancellationTokenSource.Token);

            result = (response?.IsSuccess ?? false, response?.Payload ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, VGManagerAdapterErrorMessage);
            return (false, string.Empty);
        }

        return result;
    }

    private VGManagerAdapterCommand CreateMessage(string commandType, string payload)
        => new()
        {
            InstanceId = Guid.NewGuid(),
            Destination = _kafkaConsumerConfiguration.Topic,
            CommandSource = _clientConfiguration.CommandSource,
            CommandType = commandType,
            Payload = payload,
            Timestamp = DateTime.UtcNow
        };
}
