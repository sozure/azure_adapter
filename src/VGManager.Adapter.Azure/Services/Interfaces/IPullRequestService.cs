using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Response;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IPullRequestService
{
    Task<BaseResponse<AdapterResponseModel<List<GitPRResponse>>>> GetPullRequestsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        );

    Task<BaseResponse<AdapterResponseModel<bool>>> CreatePullRequestAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        );

    Task<BaseResponse<AdapterResponseModel<bool>>> CreatePullRequestsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken
        );
}
