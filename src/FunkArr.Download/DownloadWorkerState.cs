using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed record DownloadWorkerState(
    string? Title,
    string? VideoUrl,
    string? SubtitleUrl,
    string? Channel,
    int Duration,
    long Size,
    MediaType? Category,
    WorkerStatus Status,
    DownloadPhase Phase,
    int Attempt,
    string? FailMessage,
    FailureKind? LastFailureKind,
    string RouteName,
    string? ProxyUrl,
    long BytesDownloaded,
    long CurrentTimeUs,
    double Speed)
{
    public static readonly DownloadWorkerState Empty = new(
        null, null, null, null, 0, 0, null, WorkerStatus.Initialized,
        DownloadPhase.Initialized, 0, null, null, "Direct", null, 0, 0, 0.0);

    public bool IsInitialized => Title is not null;
}

public static class DownloadWorkerStateExtensions
{
    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadInitialized evt) =>
        new(evt.Title, evt.VideoUrl, evt.SubtitleUrl, evt.Channel, evt.Duration,
            evt.Size, evt.Category.ToDomain(), WorkerStatus.Initialized,
            DownloadPhase.Initialized, 0, null, null,
            evt.RouteName, evt.ProxyUrl, 0, 0, 0.0);

    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadStarted _) =>
        state with { Status = WorkerStatus.Downloading, Phase = DownloadPhase.VideoDownload };

    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadSucceeded _) =>
        state with { Status = WorkerStatus.Completed, Phase = DownloadPhase.Completed };

    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadFaulted evt) =>
        state with
        {
            Status = WorkerStatus.Failed,
            Phase = DownloadPhase.Failed,
            FailMessage = evt.Reason,
            LastFailureKind = evt.FailureKind.ToDomain(),
        };

    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadAttemptStarted evt) =>
        state with
        {
            Attempt = evt.Attempt,
            Status = WorkerStatus.Downloading,
            Phase = DownloadPhase.VideoDownload,
            FailMessage = null,
            LastFailureKind = null,
            BytesDownloaded = 0,
            CurrentTimeUs = 0,
            Speed = 0.0,
        };

    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadPhaseChanged evt) =>
        state with { Phase = evt.Phase.ToDomain() };

    public static RecordDownload ToRecordDownload(
        this DownloadWorkerState state, Guid downloadId, TimeProvider timeProvider,
        int elapsedSeconds, DataPaths.ResolvedDownload? paths)
    {
        var completedAt = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        return new RecordDownload(
            downloadId,
            state.Title!,
            state.Category!.Value,
            state.Size,
            state.Status == WorkerStatus.Completed ? DownloadStatus.Completed : DownloadStatus.Failed,
            paths?.RelativePath,
            state.FailMessage,
            elapsedSeconds,
            completedAt);
    }
}
