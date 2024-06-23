namespace VGManager.Adapter.Models.Requests;

public record ExtendedBaseRequest : BaseRequest
{
    public string Project { get; set; } = null!;
}
