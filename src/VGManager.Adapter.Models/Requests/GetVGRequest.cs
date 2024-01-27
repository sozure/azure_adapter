namespace VGManager.Adapter.Models.Requests;

public class GetVGRequest: ExtendedBaseRequest
{
    public bool ContainsSecrets { get; set; }
    public bool FilterAsRegex { get; set; }
    public string VariableGroupFilter { get; set; } = null!;
}
