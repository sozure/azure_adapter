namespace VGManager.Adapter.Models.Requests;

public record GitPRRequest
{
    public required string Organization { get; set; }
    public required string PAT { get; set; }
    public string? Project { get; set; }
}
