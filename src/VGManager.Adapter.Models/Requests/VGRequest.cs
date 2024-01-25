
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using VGManager.Adapter.Azure.Services.Requests;

namespace VGManager.Adapter.Models.Requests;

public class VGRequest: ExtendedBaseRequest
{
    public VariableGroupParameters Params { get; set; } = null!;
    public int VariableGroupId { get; set; }
}
