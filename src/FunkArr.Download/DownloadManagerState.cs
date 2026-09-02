using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed record DownloadManagerState(
    IReadOnlyList<DownloadEntry> Queue,
    IReadOnlyList<HistoryEntry> History)
{
    public static readonly DownloadManagerState Empty = new([], []);
}

public sealed record DownloadEntry(
    Guid DownloadId,
    string Title,
    string VideoUrl,
    string? SubtitleUrl,
    string Channel,
    int Duration,
    long Size,
    string Category,
    DownloadStatus Status,
    long BytesDownloaded,
    long CurrentTimeUs,
    double Speed);

public sealed record HistoryEntry(
    Guid DownloadId,
    string Title,
    string Category,
    long Size,
    int DownloadTimeSeconds,
    string FilePath,
    DownloadStatus Status,
    string FailMessage,
    long CompletedAt);

public static class DownloadManagerStateExtensions
{
    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadQueued evt)
    {
        var entry = new DownloadEntry(
            evt.DownloadId, evt.Title, evt.VideoUrl, evt.SubtitleUrl,
            evt.Channel, evt.Duration, evt.Size, evt.Category,
            DownloadStatus.Queued, 0, 0, 0.0);

        return state with { Queue = [.. state.Queue, entry] };
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadStatusChanged evt)
    {
        var status = (DownloadStatus)evt.Status;

        if (status is DownloadStatus.Completed or DownloadStatus.Failed)
        {
            var queueEntry = state.Queue.FirstOrDefault(e => e.DownloadId == evt.DownloadId);
            if (queueEntry is null)
            {
                return state;
            }

            var historyEntry = new HistoryEntry(
                evt.DownloadId, queueEntry.Title, queueEntry.Category, queueEntry.Size,
                evt.DownloadTimeSeconds, evt.FilePath ?? "", status,
                evt.FailMessage ?? "", evt.CompletedAt);

            return state with
            {
                Queue = state.Queue.Where(e => e.DownloadId != evt.DownloadId).ToArray(),
                History = [.. state.History, historyEntry],
            };
        }

        return state with
        {
            Queue = state.Queue.Select(e => e.DownloadId == evt.DownloadId
                ? e with { Status = status }
                : e).ToArray(),
        };
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadRemoved evt) =>
        state with
        {
            Queue = state.Queue.Where(e => e.DownloadId != evt.DownloadId).ToArray(),
            History = state.History.Where(e => e.DownloadId != evt.DownloadId).ToArray(),
        };

    public static DownloadManagerState UpdateProgress(
        this DownloadManagerState state, Guid downloadId, long bytesDownloaded, long currentTimeUs, double speed) =>
        state with
        {
            Queue = state.Queue.Select(e => e.DownloadId == downloadId
                ? e with { BytesDownloaded = bytesDownloaded, CurrentTimeUs = currentTimeUs, Speed = speed }
                : e).ToArray(),
        };

    public static DownloadManagerState RequeueProcessing(this DownloadManagerState state) =>
        state with
        {
            Queue = state.Queue.Select(e => e.Status == DownloadStatus.Processing
                ? e with { Status = DownloadStatus.Queued, BytesDownloaded = 0, CurrentTimeUs = 0, Speed = 0.0 }
                : e).ToArray(),
        };

    public static int ActiveCount(this DownloadManagerState state) =>
        state.Queue.Count(e => e.Status == DownloadStatus.Processing);

    public static DownloadEntry? NextQueued(this DownloadManagerState state) =>
        state.Queue.FirstOrDefault(e => e.Status == DownloadStatus.Queued);

    public static QueueResult ToQueueResult(this DownloadManagerState state) =>
        new(state.Queue.Select(e => new QueueItem(
            e.DownloadId, e.Title, e.Status, e.Size, e.BytesDownloaded,
            e.CurrentTimeUs, e.Duration, e.Speed, e.Category)).ToArray(),
            state.Queue.Count);

    public static HistoryResult ToHistoryResult(this DownloadManagerState state) =>
        new(state.History.Select(e => new HistoryItem(
            e.DownloadId, e.Title, e.Category, e.Size, e.DownloadTimeSeconds,
            e.FilePath, e.Status, e.FailMessage, e.CompletedAt)).ToArray());
}
