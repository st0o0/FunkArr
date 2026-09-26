using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("FunkArr.Download")
                .AddSource("FunkArr.Scoring")
                .AddSource("FunkArr.Enrichment")
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter("FunkArr.Search")
                .AddMeter("FunkArr.Download")
                .AddMeter("FunkArr.Scoring")
                .AddMeter("FunkArr.Enrichment")
                .AddOtlpExporter());
    }
}
