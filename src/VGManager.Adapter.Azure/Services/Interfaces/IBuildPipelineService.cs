using Microsoft.TeamFoundation.Build.WebApi;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IBuildPipelineService
{
    Task<BaseResponse<string>> GetRepositoryIdByBuildPipelineAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<IEnumerable<BuildDefinitionReference>>> GetBuildPipelinesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterStatus>> RunBuildPipelineAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<BaseResponse<AdapterStatus>> RunBuildPipelinesAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
