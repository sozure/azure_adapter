using VGManager.Adapter.Azure.Entities;
using VGManager.Adapter.Models.Models;

namespace VGManager.Adapter.Azure.Interfaces;
public interface IProjectAdapter
{
    Task<AdapterResponseModel<IEnumerable<ProjectEntity>>> GetProjectsAsync(string baseUrl, string pat, CancellationToken cancellationToken = default);
}
