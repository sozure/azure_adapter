namespace VGManager.Adapter.Models.Requests;

public class RunBuildPipelineRequest : GetBuildPipelineRequest
{
    public string SourceBranch { get; set; } = null!;
}
