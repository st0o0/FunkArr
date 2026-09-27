using FunkArr.Messages.Download;

namespace FunkArr.Messages.Shared;

public sealed record DownloadCompletion(
    string Title,
    MediaType Category,
    long Size,
    DownloadStatus Status,
    string? RelativePath,
    string? FailMessage,
    int DownloadTimeSeconds,
    long CompletedAt);
