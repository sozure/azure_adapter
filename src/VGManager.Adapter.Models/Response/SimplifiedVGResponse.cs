namespace VGManager.Adapter.Models.Response;

public record SimplifiedVGResponse<T>
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int Id { get; set; }
    public string Description { get; set; } = null!;
    public IDictionary<string, T> Variables { get; set; } = null!;
    public string? KeyVaultName { get; set; } = null!;
}
