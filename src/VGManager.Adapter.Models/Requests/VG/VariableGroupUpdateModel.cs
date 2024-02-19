namespace VGManager.Adapter.Models.Requests.VG;

public class VariableGroupUpdateModel : VariableGroupModel
{
    public string NewValue { get; set; } = null!;
}
