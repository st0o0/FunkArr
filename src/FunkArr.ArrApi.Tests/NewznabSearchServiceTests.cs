using FunkArr.ArrApi.Newznab;
using FunkArr.Messages;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.ArrApi.Tests;

public sealed class NewznabSearchServiceTests
{
    [Fact]
    public void CapLimit_returns_null_for_null()
    {
        Assert.Null(NewznabSearchService.CapLimit(null));
    }

    [Fact]
    public void CapLimit_caps_above_max()
    {
        Assert.Equal(NewznabSearchService.MaxLimit, NewznabSearchService.CapLimit(1000));
    }

    [Fact]
    public void CapLimit_passes_through_valid_limit()
    {
        Assert.Equal(50, NewznabSearchService.CapLimit(50));
    }

    [Fact]
    public void FilterBySeasonEpisode_returns_all_when_no_tv_params()
    {
        var items = new[] { CreateItem("S01E01"), CreateItem("S02E03") };
        var result = NewznabSearchService.FilterBySeasonEpisode(items, null);
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void FilterBySeasonEpisode_filters_by_season()
    {
        var items = new[]
        {
            CreateItem("S01E01", "1", "1"),
            CreateItem("S02E01", "2", "1"),
            CreateItem("S01E02", "1", "2")
        };

        var result = NewznabSearchService.FilterBySeasonEpisode(
            items, new SearchCommand.TvParams(Season: 1, Episode: null, TvdbId: null, ImdbId: null));

        Assert.Equal(2, result.Length);
        Assert.All(result, i => Assert.Equal("1", i.Metadata?.Season));
    }

    [Theory]
    [InlineData("tvsearch", null, null, null, null, null, null, true)]
    [InlineData("movie", null, null, null, null, null, null, true)]
    [InlineData("search", null, null, null, null, null, null, true)]
    [InlineData("tvsearch", "  ", null, null, null, null, null, true)]
    [InlineData("tvsearch", null, "1", "2", null, null, null, true)]
    [InlineData("tvsearch", null, null, null, "83214", null, null, false)]
    [InlineData("tvsearch", null, null, null, null, "tt0123456", null, false)]
    [InlineData("movie", null, null, null, null, null, "603", false)]
    [InlineData("search", "Tatort", null, null, null, null, null, false)]
    [InlineData("caps", null, null, null, null, null, null, false)]
    [InlineData("get", null, null, null, null, null, null, false)]
    [InlineData(null, null, null, null, null, null, null, false)]
    public void IsWithoutSearchCriteria_detects_rss_sync_requests(
        string? t, string? q, string? season, string? ep,
        string? tvdbId, string? imdbId, string? tmdbId, bool expected)
    {
        var req = CreateRequest(t, q: q, season: season, ep: ep, tvdbId: tvdbId, imdbId: imdbId, tmdbId: tmdbId);

        Assert.Equal(expected, NewznabSearchService.IsWithoutSearchCriteria(req));
    }

    [Theory]
    [InlineData("tvsearch", "2000,2040", 5000)]
    [InlineData("movie", "5030,5040", 2000)]
    [InlineData("search", "2000,2040", 2000)]
    [InlineData("search", "2040", 2000)]
    [InlineData("search", " 2030 , 2040 ", 2000)]
    [InlineData("search", "2040,5040", 5000)]
    [InlineData("search", "5030,5040", 5000)]
    [InlineData("search", "2040,abc", 2000)]
    [InlineData("search", "abc", 5000)]
    [InlineData("search", "", 5000)]
    [InlineData("search", null, 5000)]
    public void ValidationCategory_matches_request(string t, string? cat, int expectedParentId)
    {
        var req = CreateRequest(t, cat: cat);

        Assert.Equal(expectedParentId, NewznabSearchService.ValidationCategory(req).ParentId);
    }

    [Theory]
    [InlineData("tvsearch", null, MediaType.Show)]
    [InlineData("movie", null, MediaType.Movie)]
    [InlineData("search", "2000,2040", MediaType.Movie)]
    [InlineData("search", "5030,5040", MediaType.Show)]
    [InlineData("search", "2040,5040", null)]
    [InlineData("search", null, null)]
    public void FeedMediaType_matches_request(string t, string? cat, MediaType? expected)
    {
        Assert.Equal(expected, NewznabSearchService.FeedMediaType(CreateRequest(t, cat: cat)));
    }

    [Fact]
    public void ToFeedRss_uses_stable_id_and_first_seen()
    {
        var firstSeen = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        var entry = new FunkArr.Messages.Feed.FeedEntry(
            "0123456789abcdef0123456789abcdef", firstSeen, MediaType.Show,
            new SearchResultItem("Tatort.S2026E18.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Tatort",
                "https://example.com/v.mp4", 5400, 1_000_000, 720,
                new DateTimeOffset(2026, 10, 25, 20, 15, 0, TimeSpan.Zero), 0.9,
                Metadata: new MatchMetadata(null, "2026", "18", null, null)));

        var rss = NewznabSearchService.ToFeedRss([entry], 7, 0, "http://localhost:6969", "key");

        var item = Assert.Single(rss.Channel.Items);
        Assert.Multiple(
            () => Assert.Equal("0123456789abcdef0123456789abcdef", item.Guid.Value),
            () => Assert.False(item.Guid.IsPermaLink),
            () => Assert.Equal("Fri, 30 Oct 2026 12:00:00 GMT", item.PubDate),
            () => Assert.StartsWith("http://localhost:6969/index/api?t=get&id=", item.Link),
            () => Assert.Equal("5040", Assert.Single(item.Attributes, a => a.Name == "category").Value),
            () => Assert.Equal(7, rss.Channel.Response.Total));
    }

    private static IndexerRequest CreateRequest(
        string? t,
        string? q = null,
        string? cat = null,
        string? season = null,
        string? ep = null,
        string? tvdbId = null,
        string? imdbId = null,
        string? tmdbId = null) =>
        new(t, q, cat, Offset: null, Limit: null, Id: null, season, ep, tvdbId, imdbId, tmdbId);

    private static SearchResultItem CreateItem(string title, string? season = null, string? episode = null) =>
        new(title, "channel", "topic", "", 0, 0, 0, null, 0,
            Metadata: season is not null || episode is not null
                ? new MatchMetadata(null, season, episode, null, null)
                : null);
}
