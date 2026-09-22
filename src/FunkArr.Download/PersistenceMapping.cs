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
        (PersistedDownloadPriority)(int)priority;

    public static DownloadPriority ToDomain(this PersistedDownloadPriority priority) =>
        (DownloadPriority)(int)priority;
}
