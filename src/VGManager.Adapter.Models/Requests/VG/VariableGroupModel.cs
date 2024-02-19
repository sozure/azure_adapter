namespace VGManager.Adapter.Models.Requests.VG;

public class VariableGroupModel : BaseModel
{
    public string UserName { get; set; } = null!;

    public string Project { get; set; } = null!;

    public string VariableGroupFilter { get; set; } = null!;

    public string KeyFilter { get; set; } = null!;

    public bool ContainsSecrets { get; set; }

    public bool? KeyIsRegex { get; set; }

    public bool FilterAsRegex { get; set; }

    public string? ValueFilter { get; set; }
}
