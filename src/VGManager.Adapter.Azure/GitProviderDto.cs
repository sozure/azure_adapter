using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure;

public class GitProviderDto(
    IGitRepositoryAdapter gitRepositoryAdapter, 
    IGitVersionAdapter gitVersionAdapter, 
    IGitFileAdapter gitFileAdapter
    )
{
    public IGitRepositoryAdapter GitRepositoryAdapter { get; set; } = gitRepositoryAdapter;
    public IGitVersionAdapter GitVersionAdapter { get; set; } = gitVersionAdapter;
    public IGitFileAdapter GitFileAdapter { get; set; } = gitFileAdapter;
}
