namespace VGManager.Adapter.Models.Requests;

public class PRRequest
{
    public required string Organization { get; set; }
    public required string PAT { get; set; }
    public string? Project { get; set; }
}
