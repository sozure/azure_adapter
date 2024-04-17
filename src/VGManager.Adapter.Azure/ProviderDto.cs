using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure;

public class ProviderDto(
    IBuildPipelineAdapter buildPipelineAdapter,
    IKeyVaultAdapter keyVaultAdapter,
    IProfileService profileService,
    IProjectAdapter projectAdapter,
    IReleasePipelineAdapter releasePipelineAdapter,
    IVariableGroupService variableGroupService,
    IPullRequestAdapter pullRequestAdapter
        )
{
    public IBuildPipelineAdapter BuildPipelineAdapter { get; set; } = buildPipelineAdapter;
    public IKeyVaultAdapter KeyVaultAdapter { get; set; } = keyVaultAdapter;
    public IProfileService ProfileService { get; set; } = profileService;
    public IProjectAdapter ProjectAdapter { get; set; } = projectAdapter;
    public IReleasePipelineAdapter ReleasePipelineAdapter { get; set; } = releasePipelineAdapter;
    public IVariableGroupService VariableGroupService { get; set; } = variableGroupService;
    public IPullRequestAdapter PullRequestAdapter { get; set; } = pullRequestAdapter;
}
