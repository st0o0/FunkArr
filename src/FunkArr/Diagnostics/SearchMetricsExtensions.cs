using System.Diagnostics.Metrics;

namespace FunkArr.Diagnostics;

public static class SearchMetricsExtensions
{
    public static Counter<long> AddSearchTotal(this FunkArrMetrics m) =>
        m.Meter.CreateCounter<long>("funkarr_search_total", "{request}");

    public static Histogram<double> AddSearchDuration(this FunkArrMetrics m) =>
        m.Meter.CreateHistogram<double>("funkarr_search_duration_seconds", "s");

    public static Counter<long> AddCacheHitTotal(this FunkArrMetrics m) =>
        m.Meter.CreateCounter<long>("funkarr_cache_hit_total", "{hit}");
}
