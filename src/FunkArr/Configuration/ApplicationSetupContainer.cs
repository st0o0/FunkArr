using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Sabnzbd;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class ApplicationSetupContainer : ApplicationSetupContainer<WebApplication>
{
    protected override void SetupApplication(WebApplication app)
    {
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

        app.MapIndexerApi();
        app.MapDownloadApi();
    }
}
