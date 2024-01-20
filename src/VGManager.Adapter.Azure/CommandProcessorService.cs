using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Interfaces;
using VGManager.Adapter.Kafka.Interfaces;
using VGManager.Adapter.Models;
using VGManager.Adapter.Models.Kafka;

namespace VGManager.Adapter.Azure;

public class CommandProcessorService : ICommandProcessorService
{
    private readonly IKafkaProducerService<VGManagerAdapterCommandResponse> _kafkaProducerService;
    private readonly IGitFileAdapter _gitFileAdapter;
    private readonly IBuildPipelineAdapter _buildPipelineAdapter;
    private readonly IGitRepositoryAdapter _gitRepositoryAdapter;
    private readonly IGitVersionAdapter _gitVersionAdapter;
    private readonly IKeyVaultAdapter _keyVaultAdapter;
    private readonly IProfileAdapter _profileAdapter;
    private readonly IProjectAdapter _projectAdapter;
    private readonly IReleasePipelineAdapter _releasePipelineAdapter;
    private readonly IVariableGroupAdapter _variableGroupAdapter;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;

    public CommandProcessorService(
        IKafkaProducerService<VGManagerAdapterCommandResponse> kafkaProducerService,
        IGitFileAdapter gitFileAdapter,
        IBuildPipelineAdapter buildPipelineAdapter,
        IGitRepositoryAdapter gitRepositoryAdapter,
        IGitVersionAdapter gitVersionAdapter,
        IKeyVaultAdapter keyVaultAdapter,
        IProfileAdapter profileAdapter,
        IProjectAdapter projectAdapter,
        IReleasePipelineAdapter releasePipelineAdapter,
        IVariableGroupAdapter variableGroupAdapter, 
        IMapper mapper,
        ILogger<CommandProcessorService> logger
    )
    {
        _kafkaProducerService = kafkaProducerService;
        _gitFileAdapter = gitFileAdapter;
        _buildPipelineAdapter = buildPipelineAdapter;
        _gitRepositoryAdapter = gitRepositoryAdapter;
        _gitVersionAdapter = gitVersionAdapter;
        _keyVaultAdapter = keyVaultAdapter;
        _profileAdapter = profileAdapter;
        _projectAdapter = projectAdapter;
        _releasePipelineAdapter = releasePipelineAdapter;
        _variableGroupAdapter = variableGroupAdapter;
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
                CommandTypes.CreateTagRequest => await _gitVersionAdapter.CreateTagAsync(),
                CommandTypes.GetBuildPipelinesRequest => await _buildPipelineAdapter.GetBuildPipelinesAsync(),
                CommandTypes.RunBuildPipelinesRequest => await _buildPipelineAdapter.RunBuildPipelinesAsync(),
                CommandTypes.GetBuildPipelineRequest => await _buildPipelineAdapter.GetBuildPipelineAsync(),
                CommandTypes.RunBuildPipelineRequest => await _buildPipelineAdapter.RunBuildPipelineAsync(),
                CommandTypes.GetFilePathRequest => await _gitFileAdapter.GetFilePathAsync(),
                CommandTypes.GetConfigFilesRequest => await _gitFileAdapter.GetConfigFilesAsync(),
                CommandTypes.GetAllRepositoriesRequest => await _gitRepositoryAdapter.GetAllAsync(),
                CommandTypes.GetVariablesFromConfigRequest => await _gitRepositoryAdapter.GetVariablesFromConfigAsync(),
                CommandTypes.GetBranchesRequest => await _gitVersionAdapter.GetBranchesAsync(),
                CommandTypes.GetTagsRequest => await _gitVersionAdapter.GetTagsAsync(),
                CommandTypes.GetProfileRequest => await _profileAdapter.GetProfileAsync(),
                CommandTypes.GetEnvironmentsRequest => await _releasePipelineAdapter.GetEnvironmentsAsync(),
                CommandTypes.GetVariableGroupsRequest => await _releasePipelineAdapter.GetVariableGroupsAsync(),
                CommandTypes.GetProjectsRequest => await _projectAdapter.GetProjectsAsync(),
                _ => throw new InvalidOperationException($"Invalid command type: {commandMessage.CommandType}"),
            } ; 

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
