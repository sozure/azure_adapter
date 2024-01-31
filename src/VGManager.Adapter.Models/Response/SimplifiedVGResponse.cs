using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace VGManager.Adapter.Models.Response;

public class SimplifiedVGResponse
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int Id { get; set; }
    public string Description { get; set; } = null!;
    public IDictionary<string, VariableValue> Variables { get; set; } = null!;
    public VariableGroupProviderData ProviderData { get; set; } = null!;
}
