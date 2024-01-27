using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace VGManager.Adapter.Models.Requests;

public class UpdateVGRequest : ExtendedBaseRequest
{
    public VariableGroupParameters Params { get; set; } = null!;
    public int VariableGroupId { get; set; }
}