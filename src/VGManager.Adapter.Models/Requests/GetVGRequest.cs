namespace VGManager.Adapter.Models.Requests;

public class GetVGRequest: ExtendedBaseRequest
{
    public bool? KeyIsRegex { get; set; }
    public string KeyFilter { get; set; } = null!;
    public bool ContainsSecrets { get; set; }
    public bool FilterAsRegex { get; set; }
    public string VariableGroupFilter { get; set; } = null!;
    public string[]? PotentialVariableGroups { get; set; } = null!;
}
