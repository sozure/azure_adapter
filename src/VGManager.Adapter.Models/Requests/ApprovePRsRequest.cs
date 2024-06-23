namespace VGManager.Adapter.Models.Requests;

public record ApprovePRsRequest : GitPRRequest
{
    public required Dictionary<string, int> PullRequests { get; set; }
    public required string Approver { get; set; }
    public required string ApproverId { get; set; }
}
