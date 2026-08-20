using FunkArr.DownloadClient;
using FunkArr.Indexer;
using FunkArr.RuleSet;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
        app.MapNewznabEndpoints();
        app.MapSabnzbdEndpoints();
        app.MapMatchIntelligenceEndpoints();
        app.MapRulesetEndpoints();
        app.MapQueueEndpoints();
        app.MapSetupEndpoints();

        app.MapFallbackToFile("index.html");
    }
}
