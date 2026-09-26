using System.Diagnostics.Metrics;

namespace FunkArr.Enrichment;

internal static class Telemetry
{
    internal static readonly Meter Meter = new("FunkArr.Enrichment");

    internal static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "funkarr.enrichment.requests_total", description: "Enrichment requests by API and cache status");

    internal static readonly Counter<long> Failed = Meter.CreateCounter<long>(
        "funkarr.enrichment.failed_total", description: "Enrichment failures");

    internal static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "funkarr.enrichment.duration_seconds", unit: "s", description: "Enrichment API call duration");

    internal static readonly Counter<long> ItemsEnriched = Meter.CreateCounter<long>(
        "funkarr.enrichment.items_enriched_total", description: "Items successfully enriched");

    private static int _tvdbCacheEntries;
    private static int _tmdbCacheEntries;

    static Telemetry()
    {
        Meter.CreateObservableGauge<int>(
            "funkarr.enrichment.cache_entries",
            () =>
            [
                new Measurement<int>(_tvdbCacheEntries, new KeyValuePair<string, object?>("api", "tvdb")),
                new Measurement<int>(_tmdbCacheEntries, new KeyValuePair<string, object?>("api", "tmdb")),
            ],
            description: "Enrichment cache entry count");
    }

    internal static void SetTvdbCacheEntries(int count) => _tvdbCacheEntries = count;
    internal static void SetTmdbCacheEntries(int count) => _tmdbCacheEntries = count;
}
