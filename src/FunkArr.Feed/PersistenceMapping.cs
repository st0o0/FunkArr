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
            entry.MediaType.ToPersistence(),
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

    public static PersistedCrawlOutcome ToPersistence(this CrawlOutcome value) =>
        value switch
        {
            CrawlOutcome.Completed => PersistedCrawlOutcome.Completed,
            CrawlOutcome.CompletedWithGap => PersistedCrawlOutcome.CompletedWithGap,
            CrawlOutcome.Unchanged => PersistedCrawlOutcome.Unchanged,
            CrawlOutcome.Failed => PersistedCrawlOutcome.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static CrawlOutcome ToDomain(this PersistedCrawlOutcome value) =>
        value switch
        {
            PersistedCrawlOutcome.Completed => CrawlOutcome.Completed,
            PersistedCrawlOutcome.CompletedWithGap => CrawlOutcome.CompletedWithGap,
            PersistedCrawlOutcome.Unchanged => CrawlOutcome.Unchanged,
            PersistedCrawlOutcome.Failed => CrawlOutcome.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static FeedEntry ToDomain(this PersistedFeedEntry entry) =>
        new(entry.StableId,
            entry.FirstSeenAt,
            entry.MediaType.ToDomain(),
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
