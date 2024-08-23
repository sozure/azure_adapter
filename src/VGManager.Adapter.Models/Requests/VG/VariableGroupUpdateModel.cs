namespace VGManager.Adapter.Models.Requests.VG;

public record VariableGroupUpdateModel : VariableGroupChangeModel
{
    public string NewValue { get; set; } = null!;
}
