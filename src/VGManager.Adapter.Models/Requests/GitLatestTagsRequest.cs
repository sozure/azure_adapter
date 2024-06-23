namespace VGManager.Adapter.Models.Requests;

public record GitLatestTagsRequest : BaseRequest
{
    public required Guid[] RepositoryIds { get; set; }
}
