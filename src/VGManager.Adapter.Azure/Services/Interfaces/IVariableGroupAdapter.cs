using Microsoft.TeamFoundation.DistributedTask.WebApi;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Interfaces;

public interface IVariableGroupAdapter
{
    void Setup(string organization, string project, string pat);
    Task<AdapterResponseModel<IEnumerable<VariableGroup>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AdapterStatus> UpdateAsync(VariableGroupParameters variableGroupParameters, int variableGroupId, CancellationToken cancellationToken = default);
}
