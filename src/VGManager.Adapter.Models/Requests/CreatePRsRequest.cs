namespace VGManager.Adapter.Models.Requests;

public class CreatePRsRequest : GitPRRequest
{
    public bool AutoComplete { get; set; }
    public bool ForceComplete { get; set; }
    public required string[] Repositories { get; set; }
    public required string SourceBranch { get; set; }
    public required string TargetBranch { get; set; }
    public required string Title { get; set; }
}
