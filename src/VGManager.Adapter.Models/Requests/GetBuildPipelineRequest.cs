namespace VGManager.Adapter.Models.Requests;

public record GetBuildPipelineRequest : ExtendedBaseRequest
{
    public int Id { get; set; }
}
