using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FunkArr.Download;

internal static class Telemetry
{
    internal static readonly ActivitySource Source = new("FunkArr.Download");
    internal static readonly Meter Meter = new("FunkArr.Download");

    internal static readonly Counter<long> DownloadsCompleted = Meter.CreateCounter<long>(
        "funkarr.download.completed", description: "Total completed downloads");

    internal static readonly Counter<long> DownloadsFailed = Meter.CreateCounter<long>(
        "funkarr.download.failed", description: "Total failed downloads");

    internal static readonly Histogram<double> DownloadDuration = Meter.CreateHistogram<double>(
        "funkarr.download.duration", "s", "FFmpeg download duration in seconds");
}
