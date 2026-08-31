using Akka.Hosting;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Sabnzbd;
using FunkArr.Search;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class ApplicationSetupContainer : ApplicationSetupContainer<WebApplication>
{
    protected override void SetupApplication(WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<FunkArrOptions>>().Value;
        var registry = app.Services.GetRequiredService<IActorRegistry>();
        var searchGateway = registry.Get<SearchGatewayManager>();

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

        app.MapIndexerApi(options.ApiKey, searchGateway);
        app.MapDownloadApi(options.ApiKey, options.DownloadPath);
    }
}
