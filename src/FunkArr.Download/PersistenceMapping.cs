using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Download;

internal static class PersistenceMapping
{
    public static DownloadMedia ToDomain(this PersistedDownloadMedia media) =>
        new(media.Title, media.VideoUrl, media.SubtitleUrl, media.Channel,
            media.Duration, media.Size, media.Category.ToDomain());

    public static PersistedDownloadMedia ToPersistence(this DownloadMedia media) =>
        new(media.Title, media.VideoUrl, media.SubtitleUrl, media.Channel,
            media.Duration, media.Size, media.Category.ToPersistence());

    public static DownloadCompletion ToDomain(this PersistedDownloadCompletion completion) =>
        new(completion.Title, completion.Category.ToDomain(), completion.Size,
            completion.Status.ToDomain(), completion.RelativePath, completion.FailMessage,
            completion.DownloadTimeSeconds, completion.CompletedAt);

    public static PersistedDownloadCompletion ToPersistence(this DownloadCompletion completion) =>
        new(completion.Title, completion.Category.ToPersistence(), completion.Size,
            completion.Status.ToPersistence(), completion.RelativePath, completion.FailMessage,
            completion.DownloadTimeSeconds, completion.CompletedAt);

    public static PersistedMediaType ToPersistence(this MediaType type) =>
        type switch
        {
            MediaType.Show => PersistedMediaType.Show,
            MediaType.Movie => PersistedMediaType.Movie,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static MediaType ToDomain(this PersistedMediaType type) =>
        type switch
        {
            PersistedMediaType.Show => MediaType.Show,
            PersistedMediaType.Movie => MediaType.Movie,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static PersistedDownloadPriority ToPersistence(this DownloadPriority priority) =>
        priority switch
        {
            DownloadPriority.Low => PersistedDownloadPriority.Low,
            DownloadPriority.Normal => PersistedDownloadPriority.Normal,
            DownloadPriority.High => PersistedDownloadPriority.High,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
        };

    public static DownloadPriority ToDomain(this PersistedDownloadPriority priority) =>
        priority switch
        {
            PersistedDownloadPriority.Low => DownloadPriority.Low,
            PersistedDownloadPriority.Normal => DownloadPriority.Normal,
            PersistedDownloadPriority.High => DownloadPriority.High,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
        };

    public static PersistedDownloadStatus ToPersistence(this DownloadStatus status) =>
        status switch
        {
            DownloadStatus.Queued => PersistedDownloadStatus.Queued,
            DownloadStatus.Processing => PersistedDownloadStatus.Processing,
            DownloadStatus.Completed => PersistedDownloadStatus.Completed,
            DownloadStatus.Failed => PersistedDownloadStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static DownloadStatus ToDomain(this PersistedDownloadStatus status) =>
        status switch
        {
            PersistedDownloadStatus.Queued => DownloadStatus.Queued,
            PersistedDownloadStatus.Processing => DownloadStatus.Processing,
            PersistedDownloadStatus.Completed => DownloadStatus.Completed,
            PersistedDownloadStatus.Failed => DownloadStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static PersistedFailureKind ToPersistence(this FailureKind kind) =>
        kind switch
        {
            FailureKind.Transient => PersistedFailureKind.Transient,
            FailureKind.Permanent => PersistedFailureKind.Permanent,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    public static FailureKind ToDomain(this PersistedFailureKind kind) =>
        kind switch
        {
            PersistedFailureKind.Transient => FailureKind.Transient,
            PersistedFailureKind.Permanent => FailureKind.Permanent,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
}
