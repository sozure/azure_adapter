using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Profile;
using VGManager.Adapter.Models.Models;
using VGManager.Adapter.Models.Requests;
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

    public static BaseResponse<List<string>> GetResponse(List<string> result)
        => new()
        {
            Data = result
        };

    public static BaseResponse<IEnumerable<GitRepository>> GetResponse(IEnumerable<GitRepository> result)
        => new()
        {
            Data = result
        };

    public static BaseResponse<(AdapterStatus, string)> GetResponse((AdapterStatus, string) result)
        => new()
        {
            Data = result
        };

    public static BaseResponse<Profile?> GetResponse(Profile? result)
        => new()
        {
            Data = result
        };

    public static BaseResponse<AdapterResponseModel<IEnumerable<ProjectRequest>>> GetResponse(
        AdapterResponseModel<IEnumerable<ProjectRequest>> result
        )
        => new()
        {
            Data = result
        };

    public static BaseResponse<(AdapterStatus, IEnumerable<(string, string)>)> GetResponse(
        (AdapterStatus, IEnumerable<(string, string)>) result
        )
        => new()
        {
            Data = result
        };
}
