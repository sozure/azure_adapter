namespace VGManager.Adapter.Azure.Services.Requests;

public class ReleasePipelineRequest : ExtendedBaseRequest
{
    public string RepositoryName { get; set; } = null!;
    public string ConfigFile { get; set; } = null!;
}
