namespace VGManager.Adapter.Models.Requests;

public class PRRequest
{
    public string Organization { get; set; } = string.Empty;
    public string MainPAT { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string SourceRefName { get; set; } = string.Empty;
    public string TargetRefName { get; set; } = string.Empty;   
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
