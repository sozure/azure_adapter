using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IReleasePipelineAdapter
{
    Task<(AdapterStatus, IEnumerable<string>)> GetEnvironmentsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<(AdapterStatus, IEnumerable<(string, string)>)> GetVariableGroupsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
