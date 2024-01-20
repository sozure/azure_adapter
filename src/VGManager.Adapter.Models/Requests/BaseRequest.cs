namespace VGManager.Adapter.Azure.Services.Requests;

public class BaseRequest
{
    public string Organization { get; set; } = null!;
    public string PAT { get; set; } = null!;
}
