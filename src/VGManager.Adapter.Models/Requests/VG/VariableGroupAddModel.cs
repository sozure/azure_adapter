namespace VGManager.Adapter.Models.Requests.VG;

public class VariableGroupAddModel : VariableGroupModel
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;
}
