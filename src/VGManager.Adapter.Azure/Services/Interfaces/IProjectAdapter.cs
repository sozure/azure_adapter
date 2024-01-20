using VGManager.Adapter.Azure.Services.Entities;
using VGManager.Adapter.Models.Kafka;
using VGManager.Adapter.Models.Models;

namespace VGManager.Adapter.Azure.Services.Interfaces;
public interface IProjectAdapter
{
    Task<AdapterResponseModel<IEnumerable<ProjectEntity>>> GetProjectsAsync(
        //string baseUrl, 
        //string pat, 
        VGManagerAdapterCommand command,
        CancellationToken cancellationToken = default
        );
}
