using System.Diagnostics.Metrics;

namespace FunkArr.Search;

internal static class Telemetry
{
    internal static readonly Meter Meter = new("FunkArr.Search");

    internal static readonly Histogram<double> SearchDuration = Meter.CreateHistogram<double>(
        "funkarr.search.duration", "s", "Search request duration in seconds");

    internal static readonly Histogram<int> SearchResults = Meter.CreateHistogram<int>(
        "funkarr.search.results", "{results}", "Number of search results returned");
}
