namespace VGManager.Adapter.Models.Kafka;

public static class CommandTypes
{
    public const string GetBuildPipelinesRequest = nameof(GetBuildPipelinesRequest);
    public const string RunBuildPipelinesRequest = nameof(RunBuildPipelinesRequest);
    public const string GetBuildPipelineRequest = nameof(GetBuildPipelineRequest);
    public const string RunBuildPipelineRequest = nameof(RunBuildPipelineRequest);
    public const string GetFilePathRequest = nameof(GetFilePathRequest);
    public const string GetConfigFilesRequest = nameof(GetConfigFilesRequest);
    public const string GetAllRepositoriesRequest = nameof(GetAllRepositoriesRequest);
    public const string GetVariablesFromConfigRequest = nameof(GetVariablesFromConfigRequest);
    public const string GetBranchesRequest = nameof(GetBranchesRequest);
    public const string GetTagsRequest = nameof(GetTagsRequest);
    public const string CreateTagRequest = nameof(CreateTagRequest);
    public const string GetProfileRequest = nameof(GetProfileRequest);
    public const string GetEnvironmentsRequest = nameof(GetEnvironmentsRequest);
    public const string GetVariableGroupsRequest = nameof(GetVariableGroupsRequest);
    public const string GetProjectsRequest = nameof(GetProjectsRequest);
}
