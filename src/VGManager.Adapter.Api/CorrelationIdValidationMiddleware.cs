using CorrelationId;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace VGManager.Adapter.Api;

public class CorrelationIdValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorrelationIdOptions _options;

    public CorrelationIdValidationMiddleware(RequestDelegate next, IOptions<CorrelationIdOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.RequestHeader, out var cid)
            && !StringValues.IsNullOrEmpty(cid))
        {
            var correlationId = cid.FirstOrDefault();

            if (!Guid.TryParse(correlationId, out var _))
            {
                context.Request.Headers.Remove(_options.RequestHeader);
            }
        }

        await _next(context);
    }
}
