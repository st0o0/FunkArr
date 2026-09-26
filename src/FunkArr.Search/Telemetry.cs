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
}
