using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed.Tests;

public sealed class FeedManagerStateTests
{
    private static readonly DateTimeOffset _t0 = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static FeedManagerState With(params FeedEntry[] entries) =>
        FeedManagerState.Empty.Apply(new FeedEntriesAdded([.. entries.Select(e => e.ToPersistence())]));

    [Fact]
    public void NewEntries_skips_known_ids_and_keeps_earliest_duplicate()
    {
        var state = With(TestData.Entry("a", _t0));

        var fresh = state.NewEntries(
        [
            TestData.Entry("a", _t0.AddHours(1)),
            TestData.Entry("b", _t0.AddHours(2)),
            TestData.Entry("b", _t0.AddHours(1))
        ]);

        var entry = Assert.Single(fresh);
        Assert.Multiple(
            () => Assert.Equal("b", entry.StableId),
            () => Assert.Equal(_t0.AddHours(1), entry.FirstSeenAt));
    }

    [Fact]
    public void Re_added_entry_keeps_original_first_seen()
    {
        var state = With(TestData.Entry("a", _t0));

        state = state.Apply(new FeedEntriesAdded([TestData.Entry("a", _t0.AddDays(1)).ToPersistence()]));

        var entry = Assert.Single(state.Entries.Values);
        Assert.Equal(_t0, entry.FirstSeenAt);
    }

    [Fact]
    public void Query_orders_newest_first_filters_category_and_pages()
    {
        var state = With(
            TestData.Entry("old", _t0),
            TestData.Entry("new", _t0.AddHours(2)),
            TestData.Entry("mid", _t0.AddHours(1)),
            TestData.Entry("movie", _t0.AddHours(3), MediaType.Movie));

        var page = state.Query(new QueryFeed(MediaType.Show, 1, 2));

        Assert.Multiple(
            () => Assert.Equal(3, page.Total),
            () => Assert.Equal(new[] { "mid", "old" }, page.Entries.Select(e => e.StableId).ToArray()));
    }

    [Fact]
    public void Query_without_category_returns_all()
    {
        var state = With(TestData.Entry("a", _t0), TestData.Entry("m", _t0, MediaType.Movie));

        Assert.Equal(2, state.Query(new QueryFeed(null, 0, 100)).Total);
    }

    [Fact]
    public void ExpiredIds_returns_entries_older_than_retention()
    {
        var state = With(TestData.Entry("old", _t0.AddDays(-15)), TestData.Entry("fresh", _t0.AddDays(-1)));

        var expired = state.ExpiredIds(_t0, TimeSpan.FromDays(14));

        Assert.Equal("old", Assert.Single(expired));
        Assert.Equal("fresh", Assert.Single(state.Apply(new FeedEntriesExpired(expired)).Entries.Keys));
    }

    [Fact]
    public void Query_keeps_order_and_category_totals_after_expiry()
    {
        var state = With(
            TestData.Entry("old", _t0.AddDays(-15)),
            TestData.Entry("b", _t0),
            TestData.Entry("a", _t0),
            TestData.Entry("movie", _t0.AddHours(1), MediaType.Movie));

        state = state.Apply(new FeedEntriesExpired(state.ExpiredIds(_t0, TimeSpan.FromDays(14))));

        var shows = state.Query(new QueryFeed(MediaType.Show, 0, 10));
        var all = state.Query(new QueryFeed(null, 0, 10));
        Assert.Multiple(
            () => Assert.Equal(2, shows.Total),
            () => Assert.Equal(new[] { "a", "b" }, shows.Entries.Select(e => e.StableId).ToArray()),
            () => Assert.Equal(3, all.Total),
            () => Assert.Equal(new[] { "movie", "a", "b" }, all.Entries.Select(e => e.StableId).ToArray()),
            () => Assert.Equal(3, state.Entries.Count));
    }

    [Fact]
    public void Query_for_category_without_entries_is_empty()
    {
        var state = With(TestData.Entry("a", _t0));

        var movies = state.Query(new QueryFeed(MediaType.Movie, 0, 10));

        Assert.Multiple(
            () => Assert.Equal(0, movies.Total),
            () => Assert.Empty(movies.Entries));
    }

    [Fact]
    public void ExpiredIds_returns_all_entries_past_retention_oldest_first()
    {
        var state = With(
            TestData.Entry("older", _t0.AddDays(-20)),
            TestData.Entry("old", _t0.AddDays(-15)),
            TestData.Entry("fresh", _t0.AddDays(-1)));

        var expired = state.ExpiredIds(_t0, TimeSpan.FromDays(14));

        Assert.Equal(new[] { "older", "old" }, expired);
    }

    [Fact]
    public void Persistence_roundtrip_keeps_release_fields()
    {
        var original = TestData.Entry("a", _t0);

        var restored = FeedManagerState.FromPersistence(With(original).GetPersistenceState());

        var entry = Assert.Single(restored.Entries.Values);
        Assert.Multiple(
            () => Assert.Equal(original.Item.Title, entry.Item.Title),
            () => Assert.Equal(original.Item.Url, entry.Item.Url),
            () => Assert.Equal("2026", entry.Item.Metadata?.Season),
            () => Assert.Equal("18", entry.Item.Metadata?.Episode),
            () => Assert.Equal(original.FirstSeenAt, entry.FirstSeenAt));
    }
}
