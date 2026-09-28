using System.Diagnostics.Metrics;

namespace FunkArr.Enrichment;

internal static class Telemetry
{
    private static readonly Meter _meter = new("FunkArr.Enrichment");

    internal static readonly Counter<long> Requests = _meter.CreateCounter<long>(
        "funkarr.enrichment.requests_total", description: "Enrichment requests by API and cache status");

    internal static readonly Counter<long> Failed = _meter.CreateCounter<long>(
        "funkarr.enrichment.failed_total", description: "Enrichment failures");

    internal static readonly Histogram<double> Duration = _meter.CreateHistogram<double>(
        "funkarr.enrichment.duration_seconds", unit: "s", description: "Enrichment API call duration");

    internal static readonly Counter<long> ItemsEnriched = _meter.CreateCounter<long>(
        "funkarr.enrichment.items_enriched_total", description: "Items successfully enriched");
}
