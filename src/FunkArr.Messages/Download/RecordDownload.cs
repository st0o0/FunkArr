using FunkArr.Messages.Shared;

namespace FunkArr.Messages.Download;

public sealed record RecordDownload(
    Guid DownloadId,
    DownloadCompletion Completion);
