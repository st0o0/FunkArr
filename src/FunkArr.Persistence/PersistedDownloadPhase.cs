namespace FunkArr.Persistence;

public enum PersistedDownloadPhase
{
    Initialized,
    SubtitleDownload,
    VideoDownload,
    Remuxing,
    Moving,
    Completed,
    Failed,
}
