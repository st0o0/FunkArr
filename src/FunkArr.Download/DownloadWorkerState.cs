using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed record DownloadWorkerState(
    DownloadMedia? Media,
    WorkerStatus Status,
    DownloadPhase Phase,
    int Attempt,
    string? FailMessage,
    FailureKind? LastFailureKind,
    DownloadProgress Progress)
{
    public static readonly DownloadWorkerState Empty = new(
        null, WorkerStatus.Initialized,
        DownloadPhase.Initialized, 0, null, null,
        new DownloadProgress(0, 0, 0.0));

    public bool IsInitialized => Media is not null;
}

public static class DownloadWorkerStateExtensions
{
    public static DownloadWorkerState Apply(this DownloadWorkerState state, DownloadInitialized evt) =>
        new(evt.Media.ToDomain(), WorkerStatus.Initialized,
            DownloadPhase.Initialized, 0, null, null,
            new DownloadProgress(0, 0, 0.0));

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
            Progress = new DownloadProgress(0, 0, 0.0),
        };

    public static RecordDownload ToRecordDownload(
        this DownloadWorkerState state, Guid downloadId, TimeProvider timeProvider,
        int elapsedSeconds, DataPaths.ResolvedDownload? paths)
    {
        var completedAt = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var completion = new DownloadCompletion(
            state.Media!.Title,
            state.Media.Category,
            state.Media.Size,
            state.Status == WorkerStatus.Completed ? DownloadStatus.Completed : DownloadStatus.Failed,
            paths?.RelativePath,
            state.FailMessage,
            elapsedSeconds,
            completedAt);
        return new RecordDownload(downloadId, completion);
    }
}
