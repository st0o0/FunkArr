using FunkArr.Core;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed class DownloadWorkerState
{
    public DownloadMedia? Media { get; set; }
    public WorkerStatus Status { get; set; } = WorkerStatus.Initialized;
    public DownloadPhase Phase { get; set; } = DownloadPhase.Initialized;
    public int Attempt { get; set; }
    public string? FailMessage { get; set; }
    public FailureKind? LastFailureKind { get; set; }
    public DownloadProgress Progress { get; set; } = new(0, 0, 0.0);

    public static DownloadWorkerState Empty => new();

    public bool IsInitialized => Media is not null;

    public void Apply(DownloadInitialized evt)
    {
        Media = evt.Media.ToDomain();
        Status = WorkerStatus.Initialized;
        Phase = DownloadPhase.Initialized;
        Attempt = 0;
        FailMessage = null;
        LastFailureKind = null;
        Progress = new DownloadProgress(0, 0, 0.0);
    }

    public void Apply(DownloadStarted _)
    {
        Status = WorkerStatus.Downloading;
        Phase = DownloadPhase.VideoDownload;
    }

    public void Apply(DownloadSucceeded _)
    {
        Status = WorkerStatus.Completed;
        Phase = DownloadPhase.Completed;
    }

    public void Apply(DownloadFaulted evt)
    {
        Status = WorkerStatus.Failed;
        Phase = DownloadPhase.Failed;
        FailMessage = evt.Reason;
        LastFailureKind = evt.FailureKind.ToDomain();
    }

    public void Apply(DownloadAttemptStarted evt)
    {
        Attempt = evt.Attempt;
        Status = WorkerStatus.Downloading;
        Phase = DownloadPhase.VideoDownload;
        FailMessage = null;
        LastFailureKind = null;
        Progress = new DownloadProgress(0, 0, 0.0);
    }

    public RecordDownload ToRecordDownload(
        Guid downloadId, TimeProvider timeProvider,
        int elapsedSeconds, DataPaths.ResolvedDownload? paths)
    {
        var completedAt = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var completion = new DownloadCompletion(
            Media!.Title,
            Media.Category,
            Media.Size,
            Status == WorkerStatus.Completed ? DownloadStatus.Completed : DownloadStatus.Failed,
            paths?.RelativePath,
            FailMessage,
            elapsedSeconds,
            completedAt);
        return new RecordDownload(downloadId, completion);
    }
}
