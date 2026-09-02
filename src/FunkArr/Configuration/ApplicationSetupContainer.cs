using FunkArr.Api;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Sabnzbd;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class ApplicationSetupContainer : ApplicationSetupContainer<WebApplication>
{
    protected override void SetupApplication(WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference();

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

        app.MapRuleSetApi();
        app.MapSetupApi();
        app.MapIndexerApi();
        app.MapDownloadApi();

        app.MapFallbackToFile("index.html");
    }
}
