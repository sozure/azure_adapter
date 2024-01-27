namespace VGManager.Adapter.Models.Requests;

public class GetVGRequest: ExtendedBaseRequest
{
    public bool ContainsSecrets { get; set; }
    public string VariableGroupFilter { get; set; } = null!;
}
