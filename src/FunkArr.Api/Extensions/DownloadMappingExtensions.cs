using FunkArr.Api.Models.Downloads;
using FunkArr.Messages.Download;
using static FunkArr.Messages.Download.DownloadPhaseExtensions;

namespace FunkArr.Api.Extensions;

internal static class DownloadMappingExtensions
{
    internal static DownloadQueueResponse ToApi(this QueueResult result)
    {
        var items = result.Items.Select(i => i.ToApi()).ToArray();
        var activeCount = result.Items.Count(i => i.Status == DownloadStatus.Processing);
        var queuedCount = result.Items.Count(i => i.Status == DownloadStatus.Queued);
        return new DownloadQueueResponse(items, result.TotalSlots, activeCount, queuedCount,
            result.IsPaused, result.IsScheduleActive, result.NextWindow);
    }

    internal static DownloadQueueItem ToApi(this QueueItem item)
    {
        var status = item.Status == DownloadStatus.Processing
            ? QueueStatus.Processing
            : QueueStatus.Queued;

        var phase = item.Phase is DownloadPhase.Initialized or DownloadPhase.Completed or DownloadPhase.Failed
            ? DerivePhase(item.Progress.BytesDownloaded, item.TotalBytes, item.Progress.CurrentTimeUs)
            : item.Phase;
        var percentage = phase.CalculatePercentage(item.Progress.BytesDownloaded, item.TotalBytes, item.Progress.CurrentTimeUs, item.TotalDuration);

        var elapsedSeconds = item.Progress.CurrentTimeUs / 1_000_000.0;
        var speed = elapsedSeconds > 0 ? (long)(item.Progress.BytesDownloaded / elapsedSeconds) : 0;

        var eta = "00:00:00";
        if (speed > 0 && item.TotalBytes > item.Progress.BytesDownloaded)
        {
            var remainingSeconds = (item.TotalBytes - item.Progress.BytesDownloaded) / (double)speed;
            var ts = TimeSpan.FromSeconds(Math.Min(remainingSeconds, 359999));
            eta = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        return new DownloadQueueItem(
            item.DownloadId.ToString(),
            item.Title,
            status,
            item.Channel,
            item.Category.ToApi(),
            item.HasSubtitles,
            item.TotalDuration,
            item.TotalBytes,
            item.Progress.BytesDownloaded,
            percentage,
            MapPhase(phase),
            speed,
            eta,
            item.Priority.ToApi());
    }

    internal static DownloadHistoryResponse ToApi(this HistoryResult result) =>
        new([.. result.Items.Select(i => i.ToApi())], result.TotalItems);

    internal static DownloadHistoryItem ToApi(this HistoryItem item) =>
        new(item.DownloadId.ToString(),
            item.Completion.Title,
            item.Completion.Category.ToApi(),
            item.Completion.Size,
            item.Completion.DownloadTimeSeconds,
            item.Completion.Status == DownloadStatus.Completed ? item.Completion.RelativePath : null,
            item.Completion.Status == DownloadStatus.Completed ? HistoryStatus.Completed : HistoryStatus.Failed,
            item.Completion.Status == DownloadStatus.Failed ? item.Completion.FailMessage : null,
            DateTimeOffset.FromUnixTimeSeconds(item.Completion.CompletedAt));

    private static QueuePhase MapPhase(DownloadPhase phase) => phase switch
    {
        DownloadPhase.Remuxing => QueuePhase.Remuxing,
        DownloadPhase.Moving => QueuePhase.Moving,
        _ => QueuePhase.VideoDownload
    };
}
