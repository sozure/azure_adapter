using VGManager.Adapter.Azure.Services.Requests;

namespace VGManager.Adapter.Models.Requests;

public class GitRepositoryRequest<T>: GitBaseRequest<T>
{
    public string FilePath { get; set; } = null!;

    public string Delimiter { get; set; } = null!;

    public string Branch { get; set; } = null!;

    public IEnumerable<string>? Exceptions { get; set; } = null!;
}
