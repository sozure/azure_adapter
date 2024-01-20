using VGManager.Adapter.Azure.Entities;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitVersionAdapter
{
    Task<(AdapterStatus, IEnumerable<string>)> GetBranchesAsync(
        //string organization,
        //string pat,
        //string repositoryId,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<(AdapterStatus, IEnumerable<string>)> GetTagsAsync(
        //string organization,
        //string pat,
        //Guid repositoryId,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
    Task<(AdapterStatus, string)> CreateTagAsync(
        //CreateTagEntity tagEntity,
        //string defaultBranch,
        //string sprint,
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
