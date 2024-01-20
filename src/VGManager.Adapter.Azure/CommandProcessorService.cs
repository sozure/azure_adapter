using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VGManager.Adapter.Interfaces;
using VGManager.Adapter.Kafka.Interfaces;
using VGManager.Adapter.Models;
using VGManager.Adapter.Models.Kafka;

namespace VGManager.Adapter.Azure;

public class CommandProcessorService : ICommandProcessorService
{
    private readonly IKafkaProducerService<VGManagerAdapterCommandResponse> _kafkaProducerService;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;

    public CommandProcessorService(
        IKafkaProducerService<VGManagerAdapterCommandResponse> kafkaProducerService,
        IMapper mapper,
        ILogger<CommandProcessorService> logger
    )
    {
        _kafkaProducerService = kafkaProducerService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task ProcessCommandAsync(CommandMessageBase commandMessage, CancellationToken cancellationToken = default)
    {
        VGManagerAdapterCommandResponse message;
        string? destination = null;

        try
        {
            object? result = null;
            message = _mapper.Map<VGManagerAdapterCommandResponse>(commandMessage);
            var vgManagerAdapterCommandMessage = (VGManagerAdapterCommand)commandMessage;

            destination = vgManagerAdapterCommandMessage.Destination;

            result = commandMessage.CommandType switch
            {
                _ => throw new InvalidOperationException($"Invalid command type: {commandMessage.CommandType}"),
            };

            if (result is not null)
            {
                message.Payload = JsonSerializer.Serialize(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not process command for {CommandType}", commandMessage.CommandType);

            message = new VGManagerAdapterCommandResponse
            {
                IsSuccess = false,
                CommandInstanceId = commandMessage.InstanceId
            };
        }

        await SendCommandResponseAsync(message, destination, cancellationToken);
    }

    private async Task SendCommandResponseAsync(
        CommandResponseMessage commandResponseMessage,
        string? topic = default,
        CancellationToken cancellationToken = default
    )
    {
        if (topic is not null)
        {
            await _kafkaProducerService.ProduceAsync((VGManagerAdapterCommandResponse)commandResponseMessage, topic, cancellationToken);
        }
        else
        {
            _logger.LogError("Destination topic is null");
        }
    }
}
