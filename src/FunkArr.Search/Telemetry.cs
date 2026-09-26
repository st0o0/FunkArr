using System.Diagnostics.Metrics;

namespace FunkArr.Search;

internal static class Telemetry
{
    internal static readonly Meter Meter = new("FunkArr.Search");

    internal static readonly Counter<long> SearchRequests = Meter.CreateCounter<long>(
        "funkarr.search.requests_total", description: "Total search requests");

    internal static readonly Counter<long> SearchMatches = Meter.CreateCounter<long>(
        "funkarr.search.matches_total", description: "Search requests with accepted results");

    internal static readonly Counter<long> SearchNoMatch = Meter.CreateCounter<long>(
        "funkarr.search.no_match_total", description: "Search requests with no accepted results");

    internal static readonly Histogram<double> SearchDuration = Meter.CreateHistogram<double>(
        "funkarr.search.duration_seconds", unit: "s", description: "End-to-end search duration");

    internal static readonly Counter<long> Timeouts = Meter.CreateCounter<long>(
        "funkarr.search.timeouts_total", description: "Search timeouts");

    internal static readonly Counter<long> Failed = Meter.CreateCounter<long>(
        "funkarr.search.failed_total", description: "Search failures");

    internal static readonly Counter<long> MediathekErrors = Meter.CreateCounter<long>(
        "funkarr.search.mediathek_errors_total", description: "MediathekViewWeb API errors");

    internal static readonly Histogram<double> ResultsPerRequest = Meter.CreateHistogram<double>(
        "funkarr.search.results_per_request", description: "Number of results per search request");
}
