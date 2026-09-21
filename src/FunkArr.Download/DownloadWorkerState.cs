using FunkArr.Messages;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public enum WorkerStatus
{
    Initialized,
    Downloading,
    Completed,
    Failed,
}

public sealed record DownloadWorkerSnapshot(
    string? Title,
    string? VideoUrl,
    string? SubtitleUrl,
    string? Channel,
    int Duration,
    long Size,
    MediaType? Category,
    WorkerStatus Status,
    string? FailMessage);

public sealed record DownloadWorkerState(
    string? Title,
    string? VideoUrl,
    string? SubtitleUrl,
    string? Channel,
    int Duration,
    long Size,
    MediaType? Category,
    WorkerStatus Status,
    string? FailMessage,
    long BytesDownloaded,
    long CurrentTimeUs,
    double Speed)
{
    public static readonly DownloadWorkerState Empty = new(
        null, null, null, null, 0, 0, null, WorkerStatus.Initialized, null, 0, 0, 0.0);

    public bool IsInitialized => Title is not null;

    public static DownloadWorkerState FromSnapshot(DownloadWorkerSnapshot snapshot) =>
        new(snapshot.Title, snapshot.VideoUrl, snapshot.SubtitleUrl, snapshot.Channel,
            snapshot.Duration, snapshot.Size, snapshot.Category, snapshot.Status,
            snapshot.FailMessage, 0, 0, 0.0);
}

public static class DownloadWorkerStateExtensions
{
    public static DownloadWorkerSnapshot GetSnapshot(this DownloadWorkerState state) =>
        new(state.Title, state.VideoUrl, state.SubtitleUrl, state.Channel,
            state.Duration, state.Size, state.Category, state.Status, state.FailMessage);

    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadInitialized evt) =>
        new(evt.Title, evt.VideoUrl, evt.SubtitleUrl, evt.Channel, evt.Duration,
            evt.Size, evt.Category, WorkerStatus.Initialized, null, 0, 0, 0.0);

    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadStarted _) =>
        state with { Status = WorkerStatus.Downloading };

    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadSucceeded _) =>
        state with { Status = WorkerStatus.Completed };

    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadFaulted evt) =>
        state with { Status = WorkerStatus.Failed, FailMessage = evt.Reason };
}
