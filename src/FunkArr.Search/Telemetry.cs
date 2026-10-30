using System.Diagnostics.Metrics;

namespace FunkArr.Search;

internal static class Telemetry
{
    private static readonly Meter _meter = new("FunkArr.Search");

    internal static readonly Counter<long> SearchRequests = _meter.CreateCounter<long>(
        "funkarr.search.requests_total", description: "Total search requests");

    internal static readonly Counter<long> SearchMatches = _meter.CreateCounter<long>(
        "funkarr.search.matches_total", description: "Search requests with accepted results");

    internal static readonly Counter<long> SearchNoMatch = _meter.CreateCounter<long>(
        "funkarr.search.no_match_total", description: "Search requests with no accepted results");

    internal static readonly Histogram<double> SearchDuration = _meter.CreateHistogram<double>(
        "funkarr.search.duration_seconds", unit: "s", description: "End-to-end search duration");

    internal static readonly Counter<long> Timeouts = _meter.CreateCounter<long>(
        "funkarr.search.timeouts_total", description: "Search timeouts");

    internal static readonly Counter<long> Failed = _meter.CreateCounter<long>(
        "funkarr.search.failed_total", description: "Search failures");

    internal static readonly Counter<long> MediathekErrors = _meter.CreateCounter<long>(
        "funkarr.search.mediathek_errors_total", description: "MediathekViewWeb API errors");

    internal static readonly Histogram<double> ResultsPerRequest = _meter.CreateHistogram<double>(
        "funkarr.search.results_per_request", description: "Number of results per search request");

    internal static readonly Counter<long> MediathekCacheHits = _meter.CreateCounter<long>(
        "funkarr.search.mediathek_cache_hits_total", description: "MediathekViewWeb response cache hits");

    internal static readonly Counter<long> MediathekCacheMisses = _meter.CreateCounter<long>(
        "funkarr.search.mediathek_cache_misses_total", description: "MediathekViewWeb response cache misses");
}
