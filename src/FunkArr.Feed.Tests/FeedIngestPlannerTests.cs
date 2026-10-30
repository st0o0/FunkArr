using FunkArr.Messages.Mediathek;

namespace FunkArr.Feed.Tests;

public sealed class FeedIngestPlannerTests
{
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(100, 100, false)]
    [InlineData(100, 90, false)]
    [InlineData(100, 200, true)]
    [InlineData(0, 200, true)]
    public void HasUpdate_compares_list_timestamp_with_cursor(long cursor, long listTimestamp, bool expected)
    {
        Assert.Equal(expected, FeedIngestPlanner.HasUpdate(cursor, listTimestamp));
    }

    [Fact]
    public void Window_uses_cursor_when_present()
    {
        var window = FeedIngestPlanner.Window(1_000, 5_000, TimeSpan.FromHours(48));

        Assert.Equal(new IngestWindow(1_000, 5_000), window);
    }

    [Fact]
    public void Window_uses_initial_lookback_on_first_run()
    {
        var window = FeedIngestPlanner.Window(0, 200_000, TimeSpan.FromHours(48));

        Assert.Equal(new IngestWindow(200_000 - 172_800, 200_000), window);
    }

    [Fact]
    public void EvaluatePage_collects_window_and_skips_entries_still_indexing()
    {
        var window = new IngestWindow(100, 200);
        MediathekItem[] page =
        [
            TestData.Item("indexing", filmlisteTimestamp: 250),
            TestData.Item("a", filmlisteTimestamp: 200),
            TestData.Item("b", filmlisteTimestamp: 150)
        ];

        var result = FeedIngestPlanner.EvaluatePage(page, window, pageSize: 3);

        Assert.Multiple(
            () => Assert.Equal(new[] { "a", "b" }, result.Collected.Select(i => i.Title).ToArray()),
            () => Assert.Equal(1, result.InProgress),
            () => Assert.False(result.ReachedEnd));
    }

    [Fact]
    public void EvaluatePage_stops_at_first_entry_at_or_below_cursor()
    {
        var window = new IngestWindow(100, 200);
        MediathekItem[] page =
        [
            TestData.Item("a", filmlisteTimestamp: 150),
            TestData.Item("old", filmlisteTimestamp: 100),
            TestData.Item("older", filmlisteTimestamp: 90)
        ];

        var result = FeedIngestPlanner.EvaluatePage(page, window, pageSize: 3);

        Assert.Multiple(
            () => Assert.Equal("a", Assert.Single(result.Collected).Title),
            () => Assert.True(result.ReachedEnd));
    }

    [Fact]
    public void EvaluatePage_treats_missing_timestamp_as_not_new()
    {
        var window = new IngestWindow(100, 200);
        MediathekItem[] page = [TestData.Item("missing", filmlisteTimestamp: 0)];

        var result = FeedIngestPlanner.EvaluatePage(page, window, pageSize: 1);

        Assert.Multiple(
            () => Assert.Empty(result.Collected),
            () => Assert.True(result.ReachedEnd));
    }

    [Fact]
    public void EvaluatePage_ends_on_partial_page()
    {
        var window = new IngestWindow(100, 200);
        MediathekItem[] page = [TestData.Item("a", filmlisteTimestamp: 150)];

        var result = FeedIngestPlanner.EvaluatePage(page, window, pageSize: 50);

        Assert.True(result.ReachedEnd);
    }

    [Fact]
    public void Distinct_removes_duplicates_across_pages()
    {
        var item = TestData.Item("a", url: "https://example.com/a.mp4");

        var result = FeedIngestPlanner.Distinct([item, item with { Title = "a (copy)" }, TestData.Item("b")]);

        Assert.Equal(new[] { "a", "b" }, result.Select(i => i.Title).ToArray());
    }
}
