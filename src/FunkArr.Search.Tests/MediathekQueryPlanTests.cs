using System.Text.Json;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Search.Tests;

public sealed class MediathekQueryPlanTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly long _nowSeconds = _now.ToUnixTimeSeconds();
    private const long _day = 86400;

    [Fact]
    public void First_page_always_includes_future_entries_at_requested_offset()
    {
        var plan = MediathekQueryPlan.For(Query(offset: 10, size: 3), _now);

        var root = JsonDocument.Parse(plan.FirstPage()).RootElement;

        Assert.Multiple(
            () => Assert.True(root.GetProperty("future").GetBoolean()),
            () => Assert.Equal(10, root.GetProperty("offset").GetInt32()),
            () => Assert.Equal(3, root.GetProperty("size").GetInt32()));
    }

    [Fact]
    public void Page_with_enough_aired_entries_completes_after_one_query()
    {
        var plan = MediathekQueryPlan.For(Query(size: 3), _now);

        var next = plan.Apply(Page([Aired("a1", 1), Aired("a2", 2), Aired("a3", 3)], total: 10));

        Assert.Multiple(
            () => Assert.Null(next),
            () => Assert.Equal(new[] { "a1", "a2", "a3" }, plan.ToCompleted().Items.Select(i => i.Title).ToArray()));
    }

    [Fact]
    public void Page_flooded_with_future_entries_fetches_next_page_until_aired_window_is_full()
    {
        var plan = MediathekQueryPlan.For(Query(size: 3), _now);

        var next = plan.Apply(Page([Future("f1", 30), Future("f2", 20), Aired("a1", 1)], total: 10, listTimestamp: 100));
        Assert.NotNull(next);
        var root = JsonDocument.Parse(next).RootElement;
        Assert.Equal(3, root.GetProperty("offset").GetInt32());

        var done = plan.Apply(Page([Aired("a2", 2), Aired("a3", 3), Aired("a4", 4)], total: 10, listTimestamp: 200));
        var completed = plan.ToCompleted();

        Assert.Multiple(
            () => Assert.Null(done),
            () => Assert.Equal(new[] { "f1", "f2", "a1", "a2", "a3" }, completed.Items.Select(i => i.Title).ToArray()),
            () => Assert.Equal(10, completed.Total),
            () => Assert.Equal(200, completed.ListTimestamp));
    }

    [Fact]
    public void Paging_stops_at_end_of_results()
    {
        var plan = MediathekQueryPlan.For(Query(size: 3), _now);

        var next = plan.Apply(Page([Future("f1", 1), Aired("a1", 1)], total: 2));

        Assert.Multiple(
            () => Assert.Null(next),
            () => Assert.Equal(2, plan.ToCompleted().Items.Length));
    }

    [Fact]
    public void Paging_stops_after_max_pages()
    {
        var plan = MediathekQueryPlan.For(Query(size: 2), _now);
        string? next = null;

        for (var page = 0; page < MediathekQueryPlan.MaxPages; page++)
        {
            next = plan.Apply(Page([Future($"f{page}a", 10 + page), Future($"f{page}b", 20 + page)], total: 100));
        }

        Assert.Multiple(
            () => Assert.Null(next),
            () => Assert.Equal(2 * MediathekQueryPlan.MaxPages, plan.ToCompleted().Items.Length));
    }

    [Fact]
    public void Entries_beyond_max_lead_are_dropped()
    {
        var plan = MediathekQueryPlan.For(Query(size: 3), _now);

        plan.Apply(Page([Future("garbage", 1159), Future("f1", 30), Aired("a1", 1), Aired("a2", 2), Aired("a3", 3)], total: 5));

        Assert.Equal(new[] { "f1", "a1", "a2", "a3" }, plan.ToCompleted().Items.Select(i => i.Title).ToArray());
    }

    [Fact]
    public void Duplicate_entries_across_pages_are_kept_once()
    {
        var plan = MediathekQueryPlan.For(Query(size: 2), _now);

        plan.Apply(Page([Future("f1", 5), Aired("a1", 1)], total: 10));
        plan.Apply(Page([Aired("a1", 1), Aired("a2", 2)], total: 10));

        Assert.Equal(new[] { "f1", "a1", "a2" }, plan.ToCompleted().Items.Select(i => i.Title).ToArray());
    }

    [Fact]
    public void Other_sort_orders_use_a_single_query()
    {
        var plan = MediathekQueryPlan.For(Query(size: 2, sortBy: "filmlisteTimestamp"), _now);

        var next = plan.Apply(Page([Future("f1", 5), Future("f2", 6)], total: 10));

        Assert.Multiple(
            () => Assert.False(plan.KeepsAiredWindow),
            () => Assert.Null(next),
            () => Assert.Equal(2, plan.ToCompleted().Items.Length));
    }

    private static QueryMediathek Query(int offset = 0, int size = 50, string sortBy = "timestamp") =>
        new([new MediathekQueryField(["topic"], "Tele-Gym")], sortBy, "desc", offset, size, 300, null);

    private static MediathekQueryResult Page(MediathekItem[] items, int total, long listTimestamp = 0) =>
        new(items, total, listTimestamp);

    private static MediathekItem Aired(string title, int daysAgo) => Item(title, _nowSeconds - daysAgo * _day);

    private static MediathekItem Future(string title, int daysAhead) => Item(title, _nowSeconds + daysAhead * _day);

    private static MediathekItem Item(string title, long timestamp) =>
        new("BR", "Tele-Gym", title, null, timestamp, 900, 0, null, $"https://x.com/{title}.mp4", null, null, null);
}
