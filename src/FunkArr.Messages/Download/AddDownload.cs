using FunkArr.Messages.Shared;

namespace FunkArr.Messages.Download;

public sealed record AddDownload(
    DownloadMedia Media,
    DownloadPriority Priority = DownloadPriority.Normal);
