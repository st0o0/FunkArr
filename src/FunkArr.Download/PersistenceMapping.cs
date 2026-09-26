using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Persistence;

namespace FunkArr.Download;

internal static class PersistenceMapping
{
    public static PersistedMediaType ToPersistence(this MediaType type) =>
        type switch
        {
            MediaType.Show => PersistedMediaType.Show,
            MediaType.Movie => PersistedMediaType.Movie,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

    public static MediaType ToDomain(this PersistedMediaType type) =>
        type switch
        {
            PersistedMediaType.Show => MediaType.Show,
            PersistedMediaType.Movie => MediaType.Movie,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

    public static PersistedDownloadPriority ToPersistence(this DownloadPriority priority) =>
        priority switch
        {
            DownloadPriority.Low => PersistedDownloadPriority.Low,
            DownloadPriority.Normal => PersistedDownloadPriority.Normal,
            DownloadPriority.High => PersistedDownloadPriority.High,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
        };

    public static DownloadPriority ToDomain(this PersistedDownloadPriority priority) =>
        priority switch
        {
            PersistedDownloadPriority.Low => DownloadPriority.Low,
            PersistedDownloadPriority.Normal => DownloadPriority.Normal,
            PersistedDownloadPriority.High => DownloadPriority.High,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
        };

    public static PersistedDownloadStatus ToPersistence(this DownloadStatus status) =>
        status switch
        {
            DownloadStatus.Queued => PersistedDownloadStatus.Queued,
            DownloadStatus.Processing => PersistedDownloadStatus.Processing,
            DownloadStatus.Completed => PersistedDownloadStatus.Completed,
            DownloadStatus.Failed => PersistedDownloadStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    public static DownloadStatus ToDomain(this PersistedDownloadStatus status) =>
        status switch
        {
            PersistedDownloadStatus.Queued => DownloadStatus.Queued,
            PersistedDownloadStatus.Processing => DownloadStatus.Processing,
            PersistedDownloadStatus.Completed => DownloadStatus.Completed,
            PersistedDownloadStatus.Failed => DownloadStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
}
