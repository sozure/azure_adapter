using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
using VGManager.Adapter.Models.Response;

namespace VGManager.Adapter.Azure.Services.Interfaces;
public interface IProjectService
{
    Task<BaseResponse<AdapterResponseModel<IEnumerable<ProjectRequest>>>> GetProjectsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
