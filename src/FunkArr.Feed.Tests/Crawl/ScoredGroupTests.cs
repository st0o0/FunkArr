using FunkArr.Feed.Crawl;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Tests.Crawl;

public sealed class ScoredGroupTests
{
    private static ItemGroup MakeGroup(
        params (string Url, long Timestamp)[] items)
    {
        var mediathekItems = items.Select(i =>
            TestData.Item("Title", "Tatort", i.Timestamp, url: i.Url)).ToArray();
        return new ItemGroup("tatort", MediaType.Show,
            new ExternalIds(76290, "tt0806910", null), mediathekItems);
    }

    [Fact]
    public void Accepts_high_confidence_items_with_episode_metadata()
    {
        var group = MakeGroup(("https://example.com/ep1.mp4", 1000));
        var results = new[] { TestData.Result("https://example.com/ep1.mp4", score: 0.95) };
        var scored = new ScoredGroup(group, results);

        var gated = scored.Gate(0.7);

        Assert.Multiple(
            () => Assert.Single(gated.Accepted),
            () => Assert.Equal("https://example.com/ep1.mp4", gated.Accepted[0].Url));
    }

    [Fact]
    public void Rejects_low_confidence()
    {
        var group = MakeGroup(("https://example.com/ep1.mp4", 1000));
        var results = new[] { TestData.Result("https://example.com/ep1.mp4", score: 0.3) };
        var scored = new ScoredGroup(group, results);

        var gated = scored.Gate(0.7);

        Assert.Multiple(
            () => Assert.Empty(gated.Accepted),
            () => Assert.Equal(1, gated.RejectedByReason[RejectionReason.LowConfidence]));
    }

    [Fact]
    public void Rejects_missing_episode_for_shows()
    {
        var group = MakeGroup(("https://example.com/ep1.mp4", 1000));
        var results = new[]
        {
            TestData.Result("https://example.com/ep1.mp4", season: null, episode: null)
        };
        var scored = new ScoredGroup(group, results);

        var gated = scored.Gate(0.7);

        Assert.Multiple(
            () => Assert.Empty(gated.Accepted),
            () => Assert.Equal(1, gated.RejectedByReason[RejectionReason.MissingEpisode]));
    }

    [Fact]
    public void Rejects_unknown_source_url()
    {
        var group = MakeGroup(("https://example.com/ep1.mp4", 1000));
        var results = new[] { TestData.Result("https://example.com/UNKNOWN.mp4") };
        var scored = new ScoredGroup(group, results);

        var gated = scored.Gate(0.7);

        Assert.Multiple(
            () => Assert.Empty(gated.Accepted),
            () => Assert.Equal(1, gated.RejectedByReason[RejectionReason.UnknownSource]));
    }

    [Fact]
    public void Rejects_missing_id_for_movies()
    {
        var mediathekItems = new[] { TestData.Item("Movie", "Film", 1000, url: "https://example.com/m.mp4") };
        var group = new ItemGroup("film", MediaType.Movie,
            new ExternalIds(null, null, null), mediathekItems);
        var results = new[]
        {
            TestData.Result("https://example.com/m.mp4", season: null, episode: null,
                tmdbId: null, imdbId: null)
        };
        var scored = new ScoredGroup(group, results);

        var gated = scored.Gate(0.7);

        Assert.Multiple(
            () => Assert.Empty(gated.Accepted),
            () => Assert.Equal(1, gated.RejectedByReason[RejectionReason.MissingId]));
    }

    [Fact]
    public void Rejects_missing_timestamp()
    {
        var group = MakeGroup(("https://example.com/ep1.mp4", 0));
        var results = new[] { TestData.Result("https://example.com/ep1.mp4") };
        var scored = new ScoredGroup(group, results);

        var gated = scored.Gate(0.7);

        Assert.Multiple(
            () => Assert.Empty(gated.Accepted),
            () => Assert.Equal(1, gated.RejectedByReason[RejectionReason.MissingTimestamp]));
    }

    [Fact]
    public void Multiple_results_mixed_accept_and_reject()
    {
        var group = MakeGroup(
            ("https://example.com/ep1.mp4", 1000),
            ("https://example.com/ep2.mp4", 1001));
        var results = new[]
        {
            TestData.Result("https://example.com/ep1.mp4", score: 0.95),
            TestData.Result("https://example.com/ep2.mp4", score: 0.3)
        };
        var scored = new ScoredGroup(group, results);

        var gated = scored.Gate(0.7);

        Assert.Multiple(
            () => Assert.Single(gated.Accepted),
            () => Assert.Equal(1, gated.RejectedByReason[RejectionReason.LowConfidence]));
    }
}
