using FunkArr.ArrApi.Newznab;
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
