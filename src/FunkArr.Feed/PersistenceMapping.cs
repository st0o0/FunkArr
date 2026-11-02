using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Shared;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed;

public static class PersistenceMapping
{
    public static PersistedFeedEntry ToPersistence(this FeedEntry entry) =>
        new(entry.StableId,
            entry.FirstSeenAt,
            entry.MediaType == MediaType.Movie ? PersistedMediaType.Movie : PersistedMediaType.Show,
            entry.Title,
            entry.Channel,
            entry.Topic,
            entry.Url,
            entry.Duration,
            entry.Size,
            entry.Quality,
            entry.AiredAt,
            entry.Score,
            entry.SubtitleUrl,
            entry.Ids?.TvdbId,
            entry.Ids?.ImdbId,
            entry.Ids?.TmdbId,
            entry.Season,
            entry.Episode);

    public static FeedEntry ToDomain(this PersistedFeedEntry entry) =>
        new(entry.StableId,
            entry.FirstSeenAt,
            entry.MediaType == PersistedMediaType.Movie ? MediaType.Movie : MediaType.Show,
            entry.Title,
            entry.Channel,
            entry.Topic,
            entry.Url,
            entry.Duration,
            entry.Size,
            entry.Quality,
            entry.AiredAt,
            entry.Score,
            entry.SubtitleUrl,
            new ExternalIds(entry.TvdbId, entry.ImdbId, entry.TmdbId),
            entry.Season,
            entry.Episode);
}
