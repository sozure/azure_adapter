namespace VGManager.Adapter.Models.Requests;

public class GitFileBaseRequest<T> : GitBaseRequest<T>
{
    public string Branch { get; set; } = null!;
    public string? AdditionalInformation { get; set; }
}
