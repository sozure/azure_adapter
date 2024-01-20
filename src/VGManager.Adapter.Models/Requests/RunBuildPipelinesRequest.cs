namespace VGManager.Adapter.Azure.Services.Requests;

public class RunBuildPipelinesRequest : ExtendedBaseRequest
{
    public IEnumerable<IDictionary<string, string>> Pipelines { get; set; } = null!;
}
