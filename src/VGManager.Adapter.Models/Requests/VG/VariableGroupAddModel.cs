namespace VGManager.Adapter.Models.Requests.VG;

public record VariableGroupAddModel : VariableGroupChangeModel
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;
}
