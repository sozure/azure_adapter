using System.Diagnostics.CodeAnalysis;
using CorrelationId;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using VGManager.Adapter.Api.HealthChecks;
using VGManager.Communication.Kafka.Extensions;

namespace VGManager.Adapter.Api;

[ExcludeFromCodeCoverage]
static partial class Program
{
    internal static WebApplication Configure(WebApplication app, string specificOrigins)
    {
        var configuration = app.Configuration;

        var healthCheckSettings = configuration
                .GetSection(Constants.SettingKeys.HealthChecksSettings)
                .Get<HealthChecksSettings>()
                ?? throw new InvalidOperationException("HealthChecksSettings is missing from configuration.");

        app.UseHealthChecks("/health/startup", healthCheckSettings.Port, new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("startup")
        });

        app.UseHealthChecks("/health/liveness", healthCheckSettings.Port, new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("liveness")
        });

        app.UseHealthChecks("/health/readiness", healthCheckSettings.Port, new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("readiness")
        });

        RegisterStartupReadiness(app);

        app.UseCorrelationIdValidation();
        app.UseCorrelationId();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseCors(specificOrigins);
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    private static void RegisterStartupReadiness(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        scope.ServiceProvider.GetRequiredService<StartupHealthCheck>().RegisterStartupReadiness();
    }
}
