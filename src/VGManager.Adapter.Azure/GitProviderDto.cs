using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure;

public class GitProviderDto(
    IGitRepositoryService gitRepositoryAdapter,
    IGitVersionService gitVersionAdapter,
    IGitFileService gitFileAdapter
    )
{
    public IGitRepositoryService GitRepositoryAdapter { get; set; } = gitRepositoryAdapter;
    public IGitVersionService GitVersionAdapter { get; set; } = gitVersionAdapter;
    public IGitFileService GitFileAdapter { get; set; } = gitFileAdapter;
}
