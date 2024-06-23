namespace VGManager.Adapter.Models.Requests;

public record RunBuildPipelineRequest : GetBuildPipelineRequest
{
    public string SourceBranch { get; set; } = null!;
}
