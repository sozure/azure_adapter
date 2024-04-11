namespace VGManager.Adapter.Models.Requests;

public class ApprovePRsRequest : GitPRRequest
{
    public required Dictionary<string, int> PullRequests { get; set; }
    public required string Approver { get; set; }
    public required string ApproverId { get; set; }
}
