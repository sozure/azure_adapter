namespace VGManager.Adapter.Models.Requests.VG;

public record VariableGroupAddModel : VariableGroupModel
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;
}
