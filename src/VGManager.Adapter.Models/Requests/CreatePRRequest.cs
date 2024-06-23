namespace VGManager.Adapter.Models.Requests;

public record CreatePRRequest : GitPRRequest
{
    public bool AutoComplete { get; set; }
    public required string Repository { get; set; }
    public required string SourceBranch { get; set; }
    public required string TargetBranch { get; set; }
    public required string Title { get; set; }
}
