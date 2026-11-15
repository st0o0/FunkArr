using FunkArr.Feed;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Shared;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed.Tests;

public sealed class PersistenceMappingTests
{
    [Theory]
    [InlineData(MediaType.Show, PersistedMediaType.Show)]
    [InlineData(MediaType.Movie, PersistedMediaType.Movie)]
    public void MediaType_roundtrip(MediaType domain, PersistedMediaType expected)
    {
        var persisted = domain.ToPersistence();
        Assert.Equal(expected, persisted);

        var restored = persisted.ToDomain();
        Assert.Equal(domain, restored);
    }

    [Theory]
    [InlineData(CrawlOutcome.Completed, PersistedCrawlOutcome.Completed)]
    [InlineData(CrawlOutcome.CompletedWithGap, PersistedCrawlOutcome.CompletedWithGap)]
    [InlineData(CrawlOutcome.Unchanged, PersistedCrawlOutcome.Unchanged)]
    [InlineData(CrawlOutcome.Failed, PersistedCrawlOutcome.Failed)]
    public void CrawlOutcome_roundtrip(CrawlOutcome domain, PersistedCrawlOutcome expected)
    {
        var persisted = domain.ToPersistence();
        Assert.Equal(expected, persisted);

        var restored = persisted.ToDomain();
        Assert.Equal(domain, restored);
    }

    [Fact]
    public void MediaType_enum_member_count_matches()
    {
        var domainCount = Enum.GetValues<MediaType>().Count(v => v.ToString() != "Unknown");
        var persistedCount = Enum.GetValues<PersistedMediaType>().Count(v => v.ToString() != "Unknown");
        Assert.Equal(domainCount, persistedCount);
    }

    [Fact]
    public void CrawlOutcome_enum_member_count_matches()
    {
        var domainCount = Enum.GetValues<CrawlOutcome>().Count(v => v.ToString() != "Unknown");
        var persistedCount = Enum.GetValues<PersistedCrawlOutcome>().Count(v => v.ToString() != "Unknown");
        Assert.Equal(domainCount, persistedCount);
    }

    [Fact]
    public void FeedEntry_roundtrip()
    {
        var original = new FeedEntry(
            "stable-123", DateTimeOffset.UtcNow, MediaType.Show,
            "Tatort: Sturm", "ARD", "Tatort",
            "https://example.com/video.mp4", 5400, 1073741824, 720,
            DateTimeOffset.UtcNow.AddDays(-1), 0.95, "https://example.com/sub.vtt",
            new ExternalIds(83214, "tt1234567", 12345), "2024", "5");

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Equal(original.StableId, restored.StableId);
        Assert.Equal(original.Title, restored.Title);
        Assert.Equal(original.Channel, restored.Channel);
        Assert.Equal(original.Topic, restored.Topic);
        Assert.Equal(original.Url, restored.Url);
        Assert.Equal(original.Duration, restored.Duration);
        Assert.Equal(original.Size, restored.Size);
        Assert.Equal(original.Quality, restored.Quality);
        Assert.Equal(original.Score, restored.Score);
        Assert.Equal(original.SubtitleUrl, restored.SubtitleUrl);
        Assert.Equal(original.MediaType, restored.MediaType);
        Assert.Equal(original.Season, restored.Season);
        Assert.Equal(original.Episode, restored.Episode);
        Assert.NotNull(restored.Ids);
        Assert.Equal(83214, restored.Ids.TvdbId);
        Assert.Equal("tt1234567", restored.Ids.ImdbId);
        Assert.Equal(12345, restored.Ids.TmdbId);
    }

    [Fact]
    public void FeedEntry_null_optional_fields_roundtrip()
    {
        var original = new FeedEntry(
            "stable-456", DateTimeOffset.UtcNow, MediaType.Movie,
            "Film", "ZDF", "Spielfilm",
            "https://example.com/movie.mp4", 7200, 2_000_000_000, 1080,
            null, 0.5, null, new ExternalIds(null, null, null), null, null);

        var persisted = original.ToPersistence();
        var restored = persisted.ToDomain();

        Assert.Null(restored.AiredAt);
        Assert.Null(restored.SubtitleUrl);
        Assert.Null(restored.Season);
        Assert.Null(restored.Episode);
        Assert.Null(restored.Ids?.TvdbId);
        Assert.Null(restored.Ids?.ImdbId);
        Assert.Null(restored.Ids?.TmdbId);
    }

    [Fact]
    public void Undefined_MediaType_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((MediaType)99).ToPersistence());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((PersistedMediaType)99).ToDomain());
    }

    [Fact]
    public void Undefined_CrawlOutcome_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CrawlOutcome)99).ToPersistence());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((PersistedCrawlOutcome)99).ToDomain());
    }
}
