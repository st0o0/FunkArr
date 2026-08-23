using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Scalar.AspNetCore;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class FunkArrApplicationSetup : ApplicationSetupContainer<WebApplication>
{
    protected override void SetupApplication(WebApplication app)
    {
        app.UseStaticFiles();

        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = 200,
                [HealthStatus.Degraded] = 200,
                [HealthStatus.Unhealthy] = 503,
            },
        });
        app.MapGet("/alive", () => Results.Ok("Alive"));
        app.UseHttpMetrics();
        app.MapMetrics();
        app.MapControllers();
        app.MapScalarApiReference();
        app.MapOpenApi();

        app.MapFallbackToFile("index.html");
    }
}
