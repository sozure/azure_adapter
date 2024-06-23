namespace VGManager.Adapter.Models.Requests;

public record BaseRequest
{
    public string Organization { get; set; } = null!;
    public string PAT { get; set; } = null!;
}
