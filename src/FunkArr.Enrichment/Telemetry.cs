using System.Diagnostics.Metrics;

namespace FunkArr.Enrichment;

internal static class Telemetry
{
    internal static readonly Meter Meter = new("FunkArr.Enrichment");

    internal static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "funkarr.enrichment.requests_total", description: "Enrichment requests by API and cache status");
}
