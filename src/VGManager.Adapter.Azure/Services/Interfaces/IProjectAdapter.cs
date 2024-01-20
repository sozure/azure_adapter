using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;

namespace VGManager.Adapter.Azure.Services.Interfaces;
public interface IProjectAdapter
{
    Task<AdapterResponseModel<IEnumerable<ProjectRequest>>> GetProjectsAsync(
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
