using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed;

internal static class PersistenceMapping
{
    public static PersistedFeedEntry ToPersistence(this FeedEntry entry) =>
        new(entry.StableId,
            entry.FirstSeenAt,
            entry.MediaType == MediaType.Movie ? PersistedMediaType.Movie : PersistedMediaType.Show,
            entry.Item.Title,
            entry.Item.Channel,
            entry.Item.Topic,
            entry.Item.Url,
            entry.Item.Duration,
            entry.Item.Size,
            entry.Item.Quality,
            entry.Item.AiredAt,
            entry.Item.Score,
            entry.Item.SubtitleUrl,
            entry.Item.Metadata?.Ids?.TvdbId,
            entry.Item.Metadata?.Ids?.ImdbId,
            entry.Item.Metadata?.Ids?.TmdbId,
            entry.Item.Metadata?.Season,
            entry.Item.Metadata?.Episode);

    public static FeedEntry ToDomain(this PersistedFeedEntry entry) =>
        new(entry.StableId,
            entry.FirstSeenAt,
            entry.MediaType == PersistedMediaType.Movie ? MediaType.Movie : MediaType.Show,
            new SearchResultItem(
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
                new MatchMetadata(
                    new ExternalIds(entry.TvdbId, entry.ImdbId, entry.TmdbId),
                    entry.Season,
                    entry.Episode,
                    null,
                    null)));
}
