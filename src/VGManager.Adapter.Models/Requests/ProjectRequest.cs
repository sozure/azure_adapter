using Microsoft.TeamFoundation.Core.WebApi;

namespace VGManager.Adapter.Models.Requests;

public record ProjectRequest
{
    public TeamProjectReference Project { get; set; } = null!;
    public IEnumerable<string> SubscriptionIds { get; set; } = null!;
}
