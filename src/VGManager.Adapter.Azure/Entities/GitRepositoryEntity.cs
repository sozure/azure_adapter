namespace VGManager.Adapter.Azure.Entities;

public class GitRepositoryEntity
{
    public string Organization { get; set; } = null!;

    public string Project { get; set; } = null!;

    public string PAT { get; set; } = null!;

    public string RepositoryId { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string Delimiter { get; set; } = null!;

    public string Branch { get; set; } = null!;

    public IEnumerable<string>? Exceptions { get; set; } = null!;
}
