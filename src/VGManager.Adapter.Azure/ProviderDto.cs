using VGManager.Adapter.Azure.Services.Interfaces;

namespace VGManager.Adapter.Azure;

public class ProviderDto(
    IBuildPipelineAdapter buildPipelineAdapter,
    IKeyVaultAdapter keyVaultAdapter,
    IProfileAdapter profileAdapter,
    IProjectAdapter projectAdapter,
    IReleasePipelineAdapter releasePipelineAdapter,
    IVariableGroupService variableGroupService
        )
{
    public IBuildPipelineAdapter BuildPipelineAdapter { get; set; } = buildPipelineAdapter;
    public IKeyVaultAdapter KeyVaultAdapter { get; set; } = keyVaultAdapter;
    public IProfileAdapter ProfileAdapter { get; set; } = profileAdapter;
    public IProjectAdapter ProjectAdapter { get; set; } = projectAdapter;
    public IReleasePipelineAdapter ReleasePipelineAdapter { get; set; } = releasePipelineAdapter;
    public IVariableGroupService VariableGroupService { get; set; } = variableGroupService;
}
