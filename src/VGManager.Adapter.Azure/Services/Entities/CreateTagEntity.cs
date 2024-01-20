namespace VGManager.Adapter.Azure.Services.Entities;

public class CreateTagEntity
{
    public string Organization { get; set; } = null!;
    public string PAT { get; set; } = null!;
    public string Project { get; set; } = null!;
    public Guid RepositoryId { get; set; }
    public string TagName { get; set; } = null!;
    public string UserName { get; set; } = null!;
}
