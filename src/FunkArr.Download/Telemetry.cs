using System.Diagnostics.Metrics;

namespace FunkArr.Download;

internal static class Telemetry
{
    internal static readonly Meter Meter = new("FunkArr.Download");

    internal static readonly Counter<long> DownloadsCompleted = Meter.CreateCounter<long>(
        "funkarr.download.completed_total", description: "Total completed downloads");

    internal static readonly Counter<long> DownloadsFailed = Meter.CreateCounter<long>(
        "funkarr.download.failed_total", description: "Total failed downloads");

    internal static readonly Histogram<double> DownloadDuration = Meter.CreateHistogram<double>(
        "funkarr.download.duration_seconds", "s", "Download duration in seconds");

    internal static readonly Counter<long> DownloadBytes = Meter.CreateCounter<long>(
        "funkarr.download.bytes_total", "By", "Total bytes downloaded");

    private static int _queueSize;
    private static int _activeDownloads;

    internal static readonly ObservableGauge<int> QueueSize = Meter.CreateObservableGauge(
        "funkarr.download.queue_size", () => _queueSize, description: "Current download queue depth");

    internal static readonly ObservableGauge<int> ActiveDownloads = Meter.CreateObservableGauge(
        "funkarr.download.active", () => _activeDownloads, description: "Currently active downloads");

    internal static void SetQueueSize(int size) => _queueSize = size;
    internal static void SetActiveDownloads(int count) => _activeDownloads = count;
}
