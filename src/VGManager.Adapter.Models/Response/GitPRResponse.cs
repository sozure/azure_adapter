namespace VGManager.Adapter.Models.Response;

public class GitPRResponse
{
    public required string Title { get; set; }
    public required string Repository { get; set; }
    public required string CreatedBy { get; set; }
    public required string Url { get; set; }
    public required string Project { get; set; }
    public int Days { get; set; }
    public required string Created { get; set; }
    public required string Size { get; set; }
}
