
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace VGManager.Adapter.Models.Requests;

public class VGRequest
{
    public VariableGroupParameters Params { get; set; } = null!;
    public int VariableGroupId { get; set; }
}
