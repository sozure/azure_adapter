namespace VGManager.Adapter.Models.Requests;

public record RunBuildPipelinesRequest : ExtendedBaseRequest
{
    public IEnumerable<IDictionary<string, string>> Pipelines { get; set; } = null!;
}
