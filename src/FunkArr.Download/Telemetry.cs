using System.Diagnostics.Metrics;

namespace FunkArr.Download;

internal static class Telemetry
{
    private static readonly Meter _meter = new("FunkArr.Download");

    internal static readonly Counter<long> DownloadsCompleted = _meter.CreateCounter<long>(
        "funkarr.download.completed_total", description: "Total completed downloads");

    internal static readonly Counter<long> DownloadsFailed = _meter.CreateCounter<long>(
        "funkarr.download.failed_total", description: "Total failed downloads");

    internal static readonly Histogram<double> DownloadDuration = _meter.CreateHistogram<double>(
        "funkarr.download.duration_seconds", "s", "Download duration in seconds");

    internal static readonly Counter<long> DownloadBytes = _meter.CreateCounter<long>(
        "funkarr.download.bytes_total", "By", "Total bytes downloaded");

    internal static readonly Counter<long> Enqueued = _meter.CreateCounter<long>(
        "funkarr.download.enqueued_total", description: "Total downloads enqueued");

    internal static readonly Counter<long> Cancelled = _meter.CreateCounter<long>(
        "funkarr.download.cancelled_total", description: "Total downloads cancelled");

    internal static readonly Counter<long> Retries = _meter.CreateCounter<long>(
        "funkarr.download.retries_total", description: "Total download retry attempts");

    internal static readonly Counter<long> MoveFailed = _meter.CreateCounter<long>(
        "funkarr.download.move_failed_total", description: "Total file move failures");

    internal static readonly Counter<long> SubtitleTotal = _meter.CreateCounter<long>(
        "funkarr.download.subtitle_total", description: "Total subtitle preparation attempts");

    internal static readonly Histogram<double> SubtitleDuration = _meter.CreateHistogram<double>(
        "funkarr.download.subtitle_duration_seconds", "s", "Subtitle preparation duration in seconds");

    private static int _queueSize;
    private static int _activeDownloads;
    private static int _paused;
    private static int _scheduleEnabled = 1;

    static Telemetry()
    {
        _meter.CreateObservableGauge("funkarr.download.queue_size", () => _queueSize, description: "Current download queue depth");
        _meter.CreateObservableGauge("funkarr.download.active", () => _activeDownloads, description: "Currently active downloads");
        _meter.CreateObservableGauge("funkarr.download.paused", () => _paused, description: "Whether downloads are paused");
        _meter.CreateObservableGauge("funkarr.download.schedule_enabled", () => _scheduleEnabled, description: "Whether download schedule is active");
    }

    internal static void SetQueueSize(int size) => _queueSize = size;
    internal static void SetActiveDownloads(int count) => _activeDownloads = count;
    internal static void SetPaused(bool paused) => _paused = paused ? 1 : 0;
    internal static void SetScheduleEnabled(bool enabled) => _scheduleEnabled = enabled ? 1 : 0;
}
