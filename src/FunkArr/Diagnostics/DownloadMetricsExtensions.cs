using System.Diagnostics.Metrics;

namespace FunkArr.Diagnostics;

public static class DownloadMetricsExtensions
{
    public static Counter<long> AddDownloadTotal(this FunkArrMetrics m) =>
        m.Meter.CreateCounter<long>("funkarr_download_total", "{download}");

    public static Histogram<double> AddDownloadDuration(this FunkArrMetrics m) =>
        m.Meter.CreateHistogram<double>("funkarr_download_duration_seconds", "s");

    public static Gauge<double> AddQueueDepth(this FunkArrMetrics m) =>
        m.Meter.CreateGauge<double>("funkarr_queue_depth", "{job}");
}
