namespace VGManager.Adapter.Models.Response;

public record BaseResponse<T>
{
    public T Data { get; set; } = default!;
}
