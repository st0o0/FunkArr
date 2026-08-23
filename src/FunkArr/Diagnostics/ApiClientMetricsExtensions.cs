using System.Diagnostics.Metrics;

namespace FunkArr.Diagnostics;

public static class ApiClientMetricsExtensions
{
    public static Counter<long> AddApiCallTotal(this FunkArrMetrics m) =>
        m.Meter.CreateCounter<long>("funkarr_api_call_total", "{request}");

    public static Histogram<double> AddApiCallDuration(this FunkArrMetrics m) =>
        m.Meter.CreateHistogram<double>("funkarr_api_call_duration_seconds", "s");
}
