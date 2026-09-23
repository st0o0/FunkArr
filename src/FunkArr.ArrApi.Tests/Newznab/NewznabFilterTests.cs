using FunkArr.ArrApi.Newznab;
using FunkArr.Messages.Search;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NewznabFilterTests
{
    private static SearchResultItem MakeItem(string title, string? season = null, string? episode = null) =>
        new(title, "ARD", "Tatort", "url", 3600, 1000, 1080, null, 0.9,
            Season: season, Episode: episode);

    private static readonly SearchResultItem[] _items =
    [
        MakeItem("König in Gelb", "2026", "17"),
        MakeItem("König in Gelb 720p", "2026", "17"),
        MakeItem("Die letzten Menschen", "2026", "18"),
        MakeItem("Nachtschatten", "2025", "5"),
        MakeItem("Unmatched Daily Release"),
    ];

    [Fact]
    public void Filters_by_season_and_episode()
    {
        var tvParams = new SearchCommand.TvParams(2026, 17, 83214, null);

        var result = NewznabSearchService.FilterBySeasonEpisode(_items, tvParams);

        Assert.Equal(2, result.Length);
        Assert.All(result, i => Assert.Equal("17", i.Episode));
    }

    [Fact]
    public void Filters_by_season_only()
    {
        var tvParams = new SearchCommand.TvParams(2026, null, 83214, null);

        var result = NewznabSearchService.FilterBySeasonEpisode(_items, tvParams);

        Assert.Equal(3, result.Length);
        Assert.All(result, i => Assert.Equal("2026", i.Season));
    }

    [Fact]
    public void No_filter_when_no_season()
    {
        var tvParams = new SearchCommand.TvParams(null, null, 83214, null);

        var result = NewznabSearchService.FilterBySeasonEpisode(_items, tvParams);

        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void No_filter_when_null_params()
    {
        var result = NewznabSearchService.FilterBySeasonEpisode(_items, null);

        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Unmatched_items_excluded_when_filtering()
    {
        var tvParams = new SearchCommand.TvParams(2026, 17, 83214, null);

        var result = NewznabSearchService.FilterBySeasonEpisode(_items, tvParams);

        Assert.DoesNotContain(result, i => i.Season is null);
    }

    [Fact]
    public void Pagination_reflects_filtered_count()
    {
        var tvParams = new SearchCommand.TvParams(2026, 17, 83214, null);
        var filtered = NewznabSearchService.FilterBySeasonEpisode(_items, tvParams);

        Assert.Equal(2, filtered.Length);
    }

    [Fact]
    public void Matches_zero_padded_episode_numbers()
    {
        var items = new[]
        {
            MakeItem("Folge 5", "05", "07"),
            MakeItem("Folge 5 720p", "5", "7"),
        };
        var tvParams = new SearchCommand.TvParams(5, 7, 12345, null);

        var result = NewznabSearchService.FilterBySeasonEpisode(items, tvParams);

        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void Matches_zero_padded_season_only()
    {
        var items = new[]
        {
            MakeItem("Folge A", "05", "01"),
            MakeItem("Folge B", "05", "02"),
            MakeItem("Other season", "06", "01"),
        };
        var tvParams = new SearchCommand.TvParams(5, null, 12345, null);

        var result = NewznabSearchService.FilterBySeasonEpisode(items, tvParams);

        Assert.Equal(2, result.Length);
    }
}
