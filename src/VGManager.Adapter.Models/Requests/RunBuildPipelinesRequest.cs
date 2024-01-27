namespace VGManager.Adapter.Models.Requests;

public class RunBuildPipelinesRequest : ExtendedBaseRequest
{
    public IEnumerable<IDictionary<string, string>> Pipelines { get; set; } = null!;
}
