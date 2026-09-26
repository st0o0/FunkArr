using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class TelemetrySetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        var version = typeof(TelemetrySetupContainer).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService("FunkArr", serviceVersion: version))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddProcessInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("FunkArr.Search")
                .AddMeter("FunkArr.Download")
                .AddMeter("FunkArr.Scoring")
                .AddMeter("FunkArr.Enrichment")
                .AddMeter("FunkArr.History")
                .AddMeter("FunkArr.RuleSet")
                .AddMeter("FunkArr.ExternalApi")
                .AddPrometheusExporter());
    }
}
