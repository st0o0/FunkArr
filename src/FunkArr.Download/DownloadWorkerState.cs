using FunkArr.Messages.Download;

namespace FunkArr.Download;

public sealed record DownloadWorkerState(
    StartDownload? Command,
    DownloadStatus Status,
    int? ProcessId)
{
    public static readonly DownloadWorkerState Empty = new(null, DownloadStatus.Queued, null);
}

public static class DownloadWorkerStateExtensions
{
    public static DownloadWorkerState Apply(this DownloadWorkerState state, StartDownload cmd) =>
        state with { Command = cmd, Status = DownloadStatus.Processing };

    public static DownloadWorkerState WithProcessId(this DownloadWorkerState state, int pid) =>
        state with { ProcessId = pid };

    public static DownloadWorkerState WithStatus(this DownloadWorkerState state, DownloadStatus status) =>
        state with { Status = status };
}
