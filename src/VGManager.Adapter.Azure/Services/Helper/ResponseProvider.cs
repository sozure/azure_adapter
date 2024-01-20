using Microsoft.TeamFoundation.Build.WebApi;
using VGManager.Adapter.Models.Response;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Azure.Services.Helper;

public static class ResponseProvider
{
    public static BaseResponse<AdapterStatus> GetResponse(AdapterStatus adapterStatus)
    {
        return new()
        {
            Data = adapterStatus
        };
    }

    public static BaseResponse<BuildDefinitionReference> GetResponse(BuildDefinitionReference result)
    => new()
        {
            Data = result
        };

    public static BaseResponse<IEnumerable<BuildDefinitionReference>> GetResponse(IEnumerable<BuildDefinitionReference> result)
    => new()
        {
            Data = result
        };

    public static BaseResponse<(AdapterStatus, IEnumerable<string>)> GetResponse((AdapterStatus, IEnumerable<string>) result)
    => new()
        {
            Data = result
        };
}
