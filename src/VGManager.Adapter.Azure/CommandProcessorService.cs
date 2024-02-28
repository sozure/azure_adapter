using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VGManager.Adapter.Azure.Services.Interfaces;
using VGManager.Adapter.Interfaces;
using VGManager.Adapter.Models.Kafka;
using VGManager.Communication.Kafka.Interfaces;
using VGManager.Communication.Models;

namespace VGManager.Adapter.Azure;

public class CommandProcessorService(
    IKafkaProducerService<VGManagerAdapterCommandResponse> kafkaProducerService,
    IGitFileAdapter gitFileAdapter,
    IBuildPipelineAdapter buildPipelineAdapter,
    IGitRepositoryAdapter gitRepositoryAdapter,
    IGitVersionAdapter gitVersionAdapter,
    IKeyVaultAdapter keyVaultAdapter,
    IProfileAdapter profileAdapter,
    IProjectAdapter projectAdapter,
    IReleasePipelineAdapter releasePipelineAdapter,
    IVariableGroupService variableGroupService,
    IMapper mapper,
    ILogger<CommandProcessorService> logger
    ) : ICommandProcessorService
{
    public async Task ProcessCommandAsync(CommandMessageBase commandMessage, CancellationToken cancellationToken = default)
    {
        VGManagerAdapterCommandResponse message;
        string? destination = null;

        try
        {
            object? result = null;
            message = mapper.Map<VGManagerAdapterCommandResponse>(commandMessage);
            var vgManagerAdapterCommandMessage = (VGManagerAdapterCommand)commandMessage;

            destination = vgManagerAdapterCommandMessage.Destination;

            result = commandMessage.CommandType switch
            {
                CommandTypes.CreateTagRequest => await gitVersionAdapter.CreateTagAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetBuildPipelinesRequest => await buildPipelineAdapter.GetBuildPipelinesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.RunBuildPipelinesRequest => await buildPipelineAdapter.RunBuildPipelinesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetBuildPipelineRequest => await buildPipelineAdapter.GetBuildPipelineAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.RunBuildPipelineRequest => await buildPipelineAdapter.RunBuildPipelineAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetFilePathRequest => await gitFileAdapter.GetFilePathAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetConfigFilesRequest => await gitFileAdapter.GetConfigFilesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetAllRepositoriesRequest => await gitRepositoryAdapter.GetAllAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetVariablesFromConfigRequest => await gitRepositoryAdapter.GetVariablesFromConfigAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetBranchesRequest => await gitVersionAdapter.GetBranchesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetTagsRequest => await gitVersionAdapter.GetTagsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetProfileRequest => await profileAdapter.GetProfileAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetEnvironmentsRequest => await releasePipelineAdapter.GetEnvironmentsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetVariableGroupsRequest => await releasePipelineAdapter.GetVariableGroupsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetProjectsRequest => await projectAdapter.GetProjectsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetAllVGRequest => await variableGroupService.GetAllAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.GetNumberOfFoundVGsRequest => await variableGroupService.GetNumberOfFoundVGsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.UpdateVGRequest => await variableGroupService.UpdateAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                    ),
                CommandTypes.AddVGRequest => await variableGroupService.AddVariablesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.DeleteVGRequest => await variableGroupService.DeleteVariablesAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.GetKeyVaultsRequest => await keyVaultAdapter.GetKeyVaultsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.GetSecretsRequest => await keyVaultAdapter.GetSecretsAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.DeleteSecretRequest => await keyVaultAdapter.DeleteSecretAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.AddKeyVaultSecretRequest => await keyVaultAdapter.AddKeyVaultSecretAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.RecoverSecretRequest => await keyVaultAdapter.RecoverSecretAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.GetDeletedSecretsRequest => keyVaultAdapter.GetDeletedSecrets(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                CommandTypes.GetAllSecretsRequest => await keyVaultAdapter.GetAllAsync(
                    vgManagerAdapterCommandMessage,
                    cancellationToken
                ),
                _ => throw new InvalidOperationException($"Invalid command type: {commandMessage.CommandType}"),
            };

            if (result is not null)
            {
                message.Payload = JsonSerializer.Serialize(result);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not process command for {CommandType}", commandMessage.CommandType);

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
            await kafkaProducerService.ProduceAsync((VGManagerAdapterCommandResponse)commandResponseMessage, topic, cancellationToken);
        }
        else
        {
            logger.LogError("Destination topic is null");
        }
    }
}
