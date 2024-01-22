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

    public static BaseResponse<Dictionary<string, object>> GetResponse((AdapterStatus, IEnumerable<string>) result)
    => new()
    {
        Data = new Dictionary<string, object>()
        {
            ["Status"] = result.Item1,
            ["Data"] = result.Item2
        }
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

    public static BaseResponse<Dictionary<string, object>> GetResponse((AdapterStatus, string) result)
        => new()
        {
            Data = new Dictionary<string, object>()
            {
                ["Status"] = result.Item1,
                ["Data"] = result.Item2
            }
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

    public static BaseResponse<Dictionary<string, object>> GetResponse(
        (AdapterStatus, IEnumerable<(string, string)>) result
        )
    {
        var res = new List<Dictionary<string, string>>();
        foreach(var item in result.Item2)
        {
            res.Add(new()
            {
                ["Name"] = item.Item1,
                ["Type"] = item.Item2
            });
        }
        return new()
        {
            Data = new Dictionary<string, object>()
            {
                ["Status"] = result.Item1,
                ["Data"] = res
            }
        };
    }
}

