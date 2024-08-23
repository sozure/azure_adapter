namespace VGManager.Adapter.Models.Requests.VG;

public record VariableGroupChangeModel : VariableGroupModel
{
    public ExceptionModel[]? Exceptions { get; set; } = null!;
}

public record ExceptionModel
{
    public required string VariableGroupName { get; set; }
    public string? VariableKey { get; set; }
}
