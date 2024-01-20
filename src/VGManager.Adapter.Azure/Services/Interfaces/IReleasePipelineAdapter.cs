using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IReleasePipelineAdapter
{
    Task<(AdapterStatus, IEnumerable<string>)> GetEnvironmentsAsync(
        //string organization,
        //string pat,
        //string project,
        //string repositoryName,
        //string configFile,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<(AdapterStatus, IEnumerable<(string, string)>)> GetVariableGroupsAsync(
        //string organization,
        //string pat,
        //string project,
        //string repositoryName,
        //string configFile,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
