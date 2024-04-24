namespace VGManager.Adapter.Models.Requests;

public class GitLatestTagsRequest: BaseRequest
{
    public required Guid[] RepositoryIds { get; set; }
}
