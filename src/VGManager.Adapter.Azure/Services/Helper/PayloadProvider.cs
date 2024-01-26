using System.Text.Json;

namespace VGManager.Adapter.Azure.Services.Helper;

public static class PayloadProvider<T>
{
    public static T? GetPayload(string strPayload)
    {
        T? payload;

        try
        {
            payload = JsonSerializer.Deserialize<T>(strPayload);
        }
        catch (Exception)
        {
            return default;
        }

        return payload;
    }
}
