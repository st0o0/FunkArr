using FunkArr.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class TelemetrySetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext _)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService("FunkArr", serviceVersion: VersionInfo.Version))
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
