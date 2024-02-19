using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace VGManager.Adapter.Models.Requests.VG;

public class UpdateVGRequest : ExtendedBaseRequest
{
    public VariableGroupParameters Params { get; set; } = null!;
    public int VariableGroupId { get; set; }
}
