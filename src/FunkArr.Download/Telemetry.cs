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

    internal static readonly Counter<long> Enqueued = Meter.CreateCounter<long>(
        "funkarr.download.enqueued_total", description: "Total downloads enqueued");

    internal static readonly Counter<long> Cancelled = Meter.CreateCounter<long>(
        "funkarr.download.cancelled_total", description: "Total downloads cancelled");

    internal static readonly Counter<long> Retries = Meter.CreateCounter<long>(
        "funkarr.download.retries_total", description: "Total download retry attempts");

    internal static readonly Counter<long> MoveFailed = Meter.CreateCounter<long>(
        "funkarr.download.move_failed_total", description: "Total file move failures");

    private static int _queueSize;
    private static int _activeDownloads;
    private static int _paused;
    private static int _scheduleEnabled = 1;

    internal static readonly ObservableGauge<int> QueueSize = Meter.CreateObservableGauge(
        "funkarr.download.queue_size", () => _queueSize, description: "Current download queue depth");

    internal static readonly ObservableGauge<int> ActiveDownloads = Meter.CreateObservableGauge(
        "funkarr.download.active", () => _activeDownloads, description: "Currently active downloads");

    internal static readonly ObservableGauge<int> Paused = Meter.CreateObservableGauge(
        "funkarr.download.paused", () => _paused, description: "Whether downloads are paused");

    internal static readonly ObservableGauge<int> ScheduleEnabled = Meter.CreateObservableGauge(
        "funkarr.download.schedule_enabled", () => _scheduleEnabled, description: "Whether download schedule is active");

    internal static void SetQueueSize(int size) => _queueSize = size;
    internal static void SetActiveDownloads(int count) => _activeDownloads = count;
    internal static void SetPaused(bool paused) => _paused = paused ? 1 : 0;
    internal static void SetScheduleEnabled(bool enabled) => _scheduleEnabled = enabled ? 1 : 0;
}
