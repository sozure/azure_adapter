using VGManager.Adapter.Azure.Services.Requests;

namespace VGManager.Adapter.Models.Requests;

public class CreateTagRequest : ExtendedBaseRequest
{
    public Guid RepositoryId { get; set; }
    public string TagName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string DefaultBranch { get; set; } = null!;
}
