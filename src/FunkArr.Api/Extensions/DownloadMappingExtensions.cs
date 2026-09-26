using FunkArr.Messages.Download;
using static FunkArr.Messages.Download.DownloadPhaseExtensions;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api.Extensions;

internal static class DownloadMappingExtensions
{
    internal static ApiModels.DownloadQueueResponse ToApi(this QueueResult result)
    {
        var items = result.Items.Select(i => i.ToApi()).ToArray();
        var activeCount = result.Items.Count(i => i.Status == DownloadStatus.Processing);
        var queuedCount = result.Items.Count(i => i.Status == DownloadStatus.Queued);
        return new ApiModels.DownloadQueueResponse(items, result.TotalSlots, activeCount, queuedCount,
            result.IsPaused, result.IsScheduleActive, result.NextWindow);
    }

    internal static ApiModels.DownloadQueueItem ToApi(this QueueItem item)
    {
        var status = item.Status == DownloadStatus.Processing
            ? ApiModels.QueueStatus.Processing
            : ApiModels.QueueStatus.Queued;

        var phase = item.Phase is DownloadPhase.Initialized or DownloadPhase.Completed or DownloadPhase.Failed
            ? DerivePhase(item.BytesDownloaded, item.TotalBytes, item.CurrentTimeUs)
            : item.Phase;
        var percentage = phase.CalculatePercentage(item.BytesDownloaded, item.TotalBytes, item.CurrentTimeUs, item.TotalDuration);

        var elapsedSeconds = item.CurrentTimeUs / 1_000_000.0;
        var speed = elapsedSeconds > 0 ? (long)(item.BytesDownloaded / elapsedSeconds) : 0;

        var eta = "00:00:00";
        if (speed > 0 && item.TotalBytes > item.BytesDownloaded)
        {
            var remainingSeconds = (item.TotalBytes - item.BytesDownloaded) / (double)speed;
            var ts = TimeSpan.FromSeconds(Math.Min(remainingSeconds, 359999));
            eta = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        return new ApiModels.DownloadQueueItem(
            item.DownloadId.ToString(),
            item.Title,
            status,
            item.Channel,
            (ApiModels.MediaType)(int)item.Category,
            item.HasSubtitles,
            item.TotalDuration,
            item.TotalBytes,
            item.BytesDownloaded,
            percentage,
            phase.ToString().ToLowerInvariant(),
            speed,
            eta,
            item.Priority.ToString());
    }

    internal static ApiModels.DownloadHistoryResponse ToApi(this HistoryResult result) =>
        new(result.Items.Select(i => i.ToApi()).ToArray(), result.TotalItems);

    internal static ApiModels.DownloadHistoryItem ToApi(this HistoryItem item) =>
        new(item.DownloadId.ToString(),
            item.Title,
            (ApiModels.MediaType)(int)item.Category,
            item.TotalBytes,
            item.DownloadTimeSeconds,
            item.Status == DownloadStatus.Completed ? item.RelativePath : null,
            item.Status == DownloadStatus.Completed ? ApiModels.HistoryStatus.Completed : ApiModels.HistoryStatus.Failed,
            item.Status == DownloadStatus.Failed ? item.FailMessage : null,
            DateTimeOffset.FromUnixTimeSeconds(item.CompletedAt));
}
