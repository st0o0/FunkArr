using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;

namespace FunkArr.Enrichment;

public static class Telemetry
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

    private static IMemoryCache? _cache;

    public static void SetCache(IMemoryCache cache) => _cache = cache;

    internal static readonly ObservableGauge<long> CacheEntries = _meter.CreateObservableGauge(
        "funkarr.enrichment.cache_entries", () => _cache?.GetCurrentStatistics()?.CurrentEntryCount ?? 0,
        description: "Current number of cache entries");

    internal static readonly ObservableCounter<long> CacheHits = _meter.CreateObservableCounter(
        "funkarr.enrichment.cache_hits_total", () => _cache?.GetCurrentStatistics()?.TotalHits ?? 0,
        description: "Total cache hits");

    internal static readonly ObservableCounter<long> CacheMisses = _meter.CreateObservableCounter(
        "funkarr.enrichment.cache_misses_total", () => _cache?.GetCurrentStatistics()?.TotalMisses ?? 0,
        description: "Total cache misses");
}
