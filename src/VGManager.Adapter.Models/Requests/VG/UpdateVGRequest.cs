using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace VGManager.Adapter.Models.Requests.VG;

public record UpdateVGRequest : ExtendedBaseRequest
{
    public VariableGroupParameters Params { get; set; } = null!;
    public int VariableGroupId { get; set; }
}
