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

    internal static readonly Gauge<int> QueueSize = _meter.CreateGauge<int>(
        "funkarr.download.queue_size", description: "Current download queue depth");

    internal static readonly Gauge<int> Active = _meter.CreateGauge<int>(
        "funkarr.download.active", description: "Currently active downloads");

    internal static readonly Gauge<int> Paused = _meter.CreateGauge<int>(
        "funkarr.download.paused", description: "Whether downloads are paused");

    internal static readonly Gauge<int> ScheduleEnabled = _meter.CreateGauge<int>(
        "funkarr.download.schedule_enabled", description: "Whether download schedule is active");
}
