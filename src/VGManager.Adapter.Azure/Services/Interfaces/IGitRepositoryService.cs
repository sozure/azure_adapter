using Microsoft.TeamFoundation.SourceControl.WebApi;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Response;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IGitRepositoryService
{
    Task<BaseResponse<IEnumerable<GitRepository>>> GetAllAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );

    Task<BaseResponse<List<string>>> GetVariablesFromConfigAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
