using FunkArr.Messages.Shared;

namespace FunkArr.Messages.Download;

public sealed record InitDownload(
    Guid DownloadId,
    DownloadMedia Media) : IWithDownloadId;
