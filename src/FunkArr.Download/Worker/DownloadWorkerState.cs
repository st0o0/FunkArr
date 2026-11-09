using FunkArr.Core.Data;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Worker;

public sealed class DownloadWorkerState
{
    public DownloadMedia? Media { get; private set; }
    public WorkerStatus Status { get; private set; } = WorkerStatus.Initialized;
    public DownloadPhase Phase { get; private set; } = DownloadPhase.Initialized;
    public int Attempt { get; private set; }
    public string? FailMessage { get; private set; }
    public FailureKind? LastFailureKind { get; private set; }
    public DownloadProgress Progress { get; private set; } = new(0, 0, 0.0);

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

    public void ApplyProgress(DownloadProgress progress, DownloadPhase phase, long totalSize)
    {
        Progress = progress;
        Phase = phase;
        if (Media is not null)
        {
            Media = Media with { Size = Math.Max(Media.Size, totalSize) };
        }
    }

    public void ApplyPhase(DownloadPhase phase)
    {
        Phase = phase;
    }

    public void ResetAfterRecovery()
    {
        Status = WorkerStatus.Initialized;
        Phase = DownloadPhase.Initialized;
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
