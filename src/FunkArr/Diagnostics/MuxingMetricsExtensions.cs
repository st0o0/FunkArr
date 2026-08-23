using System.Diagnostics.Metrics;

namespace FunkArr.Diagnostics;

public static class MuxingMetricsExtensions
{
    public static Histogram<double> AddMuxDuration(this FunkArrMetrics m) =>
        m.Meter.CreateHistogram<double>("funkarr_mux_duration_seconds", "s");
}
