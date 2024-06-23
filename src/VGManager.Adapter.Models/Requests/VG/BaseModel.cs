namespace VGManager.Adapter.Models.Requests.VG;

public record BaseModel
{
    public string Organization { get; set; } = null!;
    public string PAT { get; set; } = null!;
}
