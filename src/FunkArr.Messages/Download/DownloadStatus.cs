namespace FunkArr.Messages.Download;

public enum DownloadStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Extracting = 4,
    Moving = 5,
    Verifying = 6,
}
