namespace VGManager.Adapter.Models.Requests.VG;

public record VariableGroupUpdateModel : VariableGroupModel
{
    public string NewValue { get; set; } = null!;
}
