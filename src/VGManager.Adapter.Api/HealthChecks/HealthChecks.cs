using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace VGManager.Adapter.Api.HealthChecks;

public sealed class StartupHealthCheck : IHealthCheck
{
    private volatile bool _isReady;

    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public StartupHealthCheck(IHostApplicationLifetime hostApplicationLifetime)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public void RegisterStartupReadiness()
    {
        _ = _hostApplicationLifetime.ApplicationStarted.Register(() => _isReady = true);
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_isReady)
        {
            return Task.FromResult(HealthCheckResult.Healthy("The startup task has completed."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("That startup task is still running."));
    }
}
