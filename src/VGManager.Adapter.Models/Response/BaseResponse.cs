using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Models.Response;

public class BaseResponse<T>
{
    public T Data { get; set; } = default!;
}
