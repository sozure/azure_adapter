using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure;

public class ProviderDto(
    IBuildPipelineService buildPipelineAdapter,
    IKeyVaultService keyVaultAdapter,
    IProfileService profileService,
    IProjectService projectAdapter,
    IReleasePipelineService releasePipelineAdapter,
    IVariableGroupService variableGroupService,
    IPullRequestService pullRequestAdapter
        )
{
    public IBuildPipelineService BuildPipelineAdapter { get; set; } = buildPipelineAdapter;
    public IKeyVaultService KeyVaultAdapter { get; set; } = keyVaultAdapter;
    public IProfileService ProfileService { get; set; } = profileService;
    public IProjectService ProjectAdapter { get; set; } = projectAdapter;
    public IReleasePipelineService ReleasePipelineAdapter { get; set; } = releasePipelineAdapter;
    public IVariableGroupService VariableGroupService { get; set; } = variableGroupService;
    public IPullRequestService PullRequestAdapter { get; set; } = pullRequestAdapter;
}
