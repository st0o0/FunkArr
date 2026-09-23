using FunkArr.ArrApi.Newznab;
using FunkArr.Messages.Search;

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
            CreateItem("S01E02", "1", "2"),
        };

        var result = NewznabSearchService.FilterBySeasonEpisode(
            items, new SearchCommand.TvParams(Season: 1, Episode: null, TvdbId: null, ImdbId: null));

        Assert.Equal(2, result.Length);
        Assert.All(result, i => Assert.Equal("1", i.Season));
    }

    private static SearchResultItem CreateItem(string title, string? season = null, string? episode = null) =>
        new(title, "channel", "topic", "", 0, 0, 0, null, 0, Season: season, Episode: episode);
}
