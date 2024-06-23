namespace VGManager.Adapter.Models.Requests;

public record MultipleReleasePipelineRequest : BaseRequest
{
    public IEnumerable<string> Projects { get; set; } = null!;
    public string RepositoryName { get; set; } = null!;
    public string ConfigFile { get; set; } = null!;
}
