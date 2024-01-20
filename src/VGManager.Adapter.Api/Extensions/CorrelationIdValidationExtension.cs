using Microsoft.AspNetCore.Builder;

namespace VGManager.Adapter.Api.Extensions;

public static class CorrelationIdValidationExtension
{
    public static IApplicationBuilder UseCorrelationIdValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdValidationMiddleware>();
    }
}
