namespace VGManager.Adapter.Azure.Services.Requests;

public class RunBuildPipelineRequest : GetBuildPipelineRequest
{
    public string SourceBranch { get; set; } = null!;
}
