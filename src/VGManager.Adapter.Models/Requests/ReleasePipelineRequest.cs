namespace VGManager.Adapter.Models.Requests;

public record ReleasePipelineRequest : ExtendedBaseRequest
{
    public string RepositoryName { get; set; } = null!;
    public string ConfigFile { get; set; } = null!;
}
