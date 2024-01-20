using Microsoft.TeamFoundation.Build.WebApi;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IBuildPipelineAdapter
{
    Task<BuildDefinitionReference> GetBuildPipelineAsync(
        //string organization,
        //string pat,
        //string project,
        //int id,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<IEnumerable<BuildDefinitionReference>> GetBuildPipelinesAsync(
        //string organization,
        //string pat,
        //string project,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<AdapterStatus> RunBuildPipelineAsync(
        //string organization,
        //string pat,
        //string project,
        //int definitionId,
        //string sourceBranch,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<AdapterStatus> RunBuildPipelinesAsync(
        //string organization,
        //string pat,
        //string project,
        //IEnumerable<IDictionary<string, string>> pipelines,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
