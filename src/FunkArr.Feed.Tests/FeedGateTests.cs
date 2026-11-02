using FunkArr.Messages;

namespace FunkArr.Feed.Tests;

public sealed class FeedGateTests
{
    private const string _url = "https://example.com/tatort.mp4";

    [Fact]
    public void Accepts_confident_episode_with_stable_id_and_first_seen()
    {
        var source = TestData.Item(url: _url, filmlisteTimestamp: 1_761_800_000);

        var result = FeedGate.Evaluate([TestData.Result(_url)], [source], MediaType.Show, 0.7);

        var entry = Assert.Single(result.Accepted);
        Assert.Multiple(
            () => Assert.Equal(FeedGate.StableIdFor("ARD", _url), entry.StableId),
            () => Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_761_800_000), entry.FirstSeenAt),
            () => Assert.Equal(MediaType.Show, entry.MediaType),
            () => Assert.Empty(result.RejectedByReason));
    }

    [Theory]
    [InlineData(0.5, "2026", "18", FeedGate.LowConfidence)]
    [InlineData(0.9, null, "18", FeedGate.MissingEpisode)]
    [InlineData(0.9, "2026", null, FeedGate.MissingEpisode)]
    public void Rejects_unsafe_tv_releases(double score, string? season, string? episode, string reason)
    {
        var source = TestData.Item(url: _url);

        var result = FeedGate.Evaluate([TestData.Result(_url, score, season, episode)], [source], MediaType.Show, 0.7);

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
        var source = TestData.Item(url: _url, topic: "Das Boot");

        var result = FeedGate.Evaluate(
            [TestData.Result(_url, season: null, episode: null, tmdbId: tmdbId, imdbId: imdbId)],
            [source], MediaType.Movie, 0.7);

        Assert.Equal(accepted ? 1 : 0, result.Accepted.Length);
    }

    [Fact]
    public void Rejects_result_without_matching_source()
    {
        var result = FeedGate.Evaluate([TestData.Result("https://example.com/other.mp4")], [TestData.Item(url: _url)],
            MediaType.Show, 0.7);

        Assert.Equal(1, result.RejectedByReason[FeedGate.UnknownSource]);
    }

    [Fact]
    public void Stable_id_ignores_title_and_channel_case()
    {
        Assert.Multiple(
            () => Assert.Equal(FeedGate.StableIdFor("ARD", _url), FeedGate.StableIdFor("ard", _url)),
            () => Assert.Equal(32, FeedGate.StableIdFor("ARD", _url).Length),
            () => Assert.NotEqual(FeedGate.StableIdFor("ARD", _url), FeedGate.StableIdFor("ARD", _url + "?v=2")));
    }
}
