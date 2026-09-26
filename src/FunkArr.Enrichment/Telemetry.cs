using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FunkArr.Enrichment;

internal static class Telemetry
{
    internal static readonly ActivitySource Source = new("FunkArr.Enrichment");
    internal static readonly Meter Meter = new("FunkArr.Enrichment");

    internal static readonly Counter<long> CacheHits = Meter.CreateCounter<long>(
        "funkarr.enrichment.cache_hits", description: "Enrichment cache hits");

    internal static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>(
        "funkarr.enrichment.cache_misses", description: "Enrichment cache misses");
}
