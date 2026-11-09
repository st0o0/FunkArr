using FunkArr.Messages;
using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Tests;

public sealed class FeedGateTests
{
    private const string Url = "https://example.com/tatort.mp4";

    [Fact]
    public void Accepts_confident_episode_with_stable_id_and_first_seen()
    {
        var source = TestData.Item(url: Url, filmlisteTimestamp: 1_761_800_000);

        var result = FeedGate.Evaluate([TestData.Result(Url)], [source], MediaType.Show, 0.7);

        var entry = Assert.Single(result.Accepted);
        Assert.Multiple(
            () => Assert.Equal(FeedGate.StableIdFor("ARD", Url), entry.StableId),
            () => Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_761_800_000), entry.FirstSeenAt),
            () => Assert.Equal(MediaType.Show, entry.MediaType),
            () => Assert.Empty(result.RejectedByReason));
    }

    [Theory]
    [InlineData(0.5, "2026", "18", RejectionReason.LowConfidence)]
    [InlineData(0.9, null, "18", RejectionReason.MissingEpisode)]
    [InlineData(0.9, "2026", null, RejectionReason.MissingEpisode)]
    public void Rejects_unsafe_tv_releases(double score, string? season, string? episode, RejectionReason reason)
    {
        var source = TestData.Item(url: Url);

        var result = FeedGate.Evaluate([TestData.Result(Url, score, season, episode)], [source], MediaType.Show, 0.7);

        Assert.Multiple(
            () => Assert.Empty(result.Accepted),
            () => Assert.Equal(1, result.RejectedByReason[reason]));
    }

    [Theory]
    [InlineData(603, null, true)]
    [InlineData(null, "tt0082096", true)]
    [InlineData(null, null, false)]
    public void Movies_need_tmdb_or_imdb_id(int? tmdbId, string? imdbId, bool accepted)
    {
        var source = TestData.Item(url: Url, topic: "Das Boot");

        var result = FeedGate.Evaluate(
            [TestData.Result(Url, season: null, episode: null, tmdbId: tmdbId, imdbId: imdbId)],
            [source], MediaType.Movie, 0.7);

        Assert.Equal(accepted ? 1 : 0, result.Accepted.Length);
    }

    [Fact]
    public void Rejects_result_without_matching_source()
    {
        var result = FeedGate.Evaluate([TestData.Result("https://example.com/other.mp4")], [TestData.Item(url: Url)],
            MediaType.Show, 0.7);

        Assert.Equal(1, result.RejectedByReason[RejectionReason.UnknownSource]);
    }

    [Fact]
    public void Stable_id_ignores_title_and_channel_case()
    {
        Assert.Multiple(
            () => Assert.Equal(FeedGate.StableIdFor("ARD", Url), FeedGate.StableIdFor("ard", Url)),
            () => Assert.Equal(32, FeedGate.StableIdFor("ARD", Url).Length),
            () => Assert.NotEqual(FeedGate.StableIdFor("ARD", Url), FeedGate.StableIdFor("ARD", Url + "?v=2")));
    }
}
