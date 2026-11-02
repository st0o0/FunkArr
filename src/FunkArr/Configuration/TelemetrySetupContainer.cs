using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class TelemetrySetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext _)
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
                .AddMeter("FunkArr.Feed")
                .AddMeter("System.Net.Http")
                .AddPrometheusExporter());
    }
}
