using System.Net.Mime;
using FunkArr.Api;
using FunkArr.Api.HealthChecks;
using FunkArr.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class SearchSetupContainer : ApplicationSetupContainer<WebApplication>, IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient(HttpClientNames.MediathekViewWeb, client =>
        {
            client.BaseAddress = new Uri("https://mediathekviewweb.de/api/query");
            client.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
        })
        .AddHttpMessageHandler(() => new ExternalApiMetricsHandler("mediathekviewweb"))
        .AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHealthChecks()
            .AddCheck<MediathekViewWebHealthCheck>("mediathekviewweb", failureStatus: HealthStatus.Degraded);
    }

    protected override void SetupApplication(WebApplication app)
    {
        app.MapMediathekApi();
    }
}
