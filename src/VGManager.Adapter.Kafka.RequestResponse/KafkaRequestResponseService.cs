using CorrelationId;
using CorrelationId.Abstractions;
using Microsoft.Extensions.Logging;
using VGManager.Adapter.Kafka.Interfaces;
using VGManager.Adapter.Kafka.RequestResponse.Interfaces;
using VGManager.Adapter.Messaging.Models.Interfaces;

namespace VGManager.Adapter.Kafka.RequestResponse;

public class KafkaRequestResponseService<TRequest, TResponse> : IKafkaRequestResponseService<TRequest, TResponse>
    where TRequest : ICommandRequest
    where TResponse : class
{
    private readonly IKafkaProducerService<TRequest> _kafkaProducerService;
    private readonly IRequestStoreService<TResponse> _store;
    private readonly ILogger<KafkaRequestResponseService<TRequest, TResponse>> _logger;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private const string ErrorMessageFormat =
        $"KafkaRequestResponseService error when requesting message with Id: {{Id}}. " +
        $"Request type: {{RequestType}}. " +
        $"Response type: {{ResponseType}}.";
    private const string TimeoutErrorMessageFormat =
        $"KafkaRequestResponseService Timeout error when requesting message with Id: {{Id}}. " +
        $"Request type: {{RequestType}}. " +
        $"Response type: {{ResponseType}}.";

    public KafkaRequestResponseService(
        IKafkaProducerService<TRequest> kafkaProducerService,
        IRequestStoreService<TResponse> store,
        ILogger<KafkaRequestResponseService<TRequest, TResponse>> logger,
        ICorrelationContextAccessor correlationContextAccessor
    )
    {
        _kafkaProducerService = kafkaProducerService;
        _store = store;
        _logger = logger;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public async Task<TResponse?> SendAndReceiveAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        TResponse? result = default;

        var correlationId = _correlationContextAccessor.CorrelationContext?.CorrelationId ?? Guid.NewGuid().ToString();

        _correlationContextAccessor.CorrelationContext = new CorrelationContext(correlationId, CorrelationIdOptions.DefaultHeader);

        using var loggerScope = _logger.BeginScope("{@CorrelationId}", _correlationContextAccessor.CorrelationContext.CorrelationId);

        try
        {
            var taskCompletionSource = new TaskCompletionSource<TResponse>();

            using (cancellationToken.Register(() =>
            {
                _store.Remove(request.InstanceId);
                taskCompletionSource.TrySetCanceled();
            }, useSynchronizationContext: false))
            {
                if (_store.Add(request.InstanceId, taskCompletionSource))
                {
                    _logger.LogDebug("Sending request to kafka: {@Request}", request);
                    await _kafkaProducerService.ProduceAsync(request, cancellationToken);

                    result = await taskCompletionSource.Task;
                }
                else
                {
                    _logger.LogWarning("Could not add request to request store: {@Request}. Skipping process.", request);
                }
            }
        }
        catch (TaskCanceledException tex)
        {
            _store.Remove(request.InstanceId);
            _logger.LogError(tex, TimeoutErrorMessageFormat, request.InstanceId, typeof(TRequest), typeof(TResponse));
            result = default;
        }
        catch (Exception ex)
        {
            _store.Remove(request.InstanceId);
            _logger.LogError(ex, ErrorMessageFormat, request.InstanceId, typeof(TRequest), typeof(TResponse));
            result = default;
        }

        return result;
    }
}
