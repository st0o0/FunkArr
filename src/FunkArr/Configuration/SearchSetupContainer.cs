using System.Net.Mime;
using FunkArr.Api;
using FunkArr.Api.HealthChecks;
using FunkArr.Search.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Terp.Diagnostics;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class SearchSetupContainer : ISetup<IServiceCollection>, ISetup<WebApplication>
{
    public void Setup(IServiceCollection services, SetupContext _)
    {
        services.AddHttpClient<MediathekClient>(client =>
        {
            client.BaseAddress = new Uri("https://mediathekviewweb.de/api/query");
            client.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
        })
        .AddHttpMessageHandler(() => new TerpHttpMetricsHandler("mediathekviewweb"))
        .AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
            options.CircuitBreaker.FailureRatio = 0.20;
        });

        services.AddHealthChecks()
            .AddCheck<MediathekViewWebHealthCheck>("mediathekviewweb", failureStatus: HealthStatus.Degraded);
    }

    public void Setup(WebApplication app, SetupContext _) => app.MapMediathekApi();
}
