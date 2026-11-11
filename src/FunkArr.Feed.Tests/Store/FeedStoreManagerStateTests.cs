using FunkArr.Feed.Store;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed.Tests.Store;

public sealed class FeedStoreManagerStateTests
{
    private static readonly DateTimeOffset _t0 = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static FeedStoreManagerState With(params FeedEntry[] entries)
    {
        var state = new FeedStoreManagerState();
        state.Apply(new FeedEntriesAdded([.. entries.Select(e => e.ToPersistence())]));
        return state;
    }

    private static FeedEntry[] QueryAll(FeedStoreManagerState state) =>
        state.Query(new QueryFeed(null, 0, 10_000)).Entries;

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

        state.Apply(new FeedEntriesAdded([TestData.Entry("a", _t0.AddDays(1)).ToPersistence()]));

        var entry = Assert.Single(QueryAll(state));
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
            () => Assert.Equal(4, page.Total),
            () => Assert.Equal(3, page.FilteredTotal),
            () => Assert.Equal(new[] { "mid", "old" }, page.Entries.Select(e => e.StableId).ToArray()));
    }

    [Fact]
    public void Query_without_category_returns_all()
    {
        var state = With(TestData.Entry("a", _t0), TestData.Entry("m", _t0, MediaType.Movie));

        var result = state.Query(new QueryFeed(null, 0, 100));
        Assert.Multiple(
            () => Assert.Equal(2, result.Total),
            () => Assert.Equal(2, result.FilteredTotal));
    }

    [Fact]
    public void ExpiredIds_returns_entries_older_than_retention()
    {
        var state = With(TestData.Entry("old", _t0.AddDays(-15)), TestData.Entry("fresh", _t0.AddDays(-1)));

        var expired = state.ExpiredIds(_t0, TimeSpan.FromDays(14));

        Assert.Equal("old", Assert.Single(expired));
        state.Apply(new FeedEntriesExpired(expired));
        Assert.Multiple(
            () => Assert.Equal(1, state.Count),
            () => Assert.Equal("fresh", Assert.Single(QueryAll(state)).StableId));
    }

    [Fact]
    public void Query_keeps_order_and_category_totals_after_expiry()
    {
        var state = With(
            TestData.Entry("old", _t0.AddDays(-15)),
            TestData.Entry("b", _t0),
            TestData.Entry("a", _t0),
            TestData.Entry("movie", _t0.AddHours(1), MediaType.Movie));

        state.Apply(new FeedEntriesExpired(state.ExpiredIds(_t0, TimeSpan.FromDays(14))));

        var shows = state.Query(new QueryFeed(MediaType.Show, 0, 10));
        var all = state.Query(new QueryFeed(null, 0, 10));
        Assert.Multiple(
            () => Assert.Equal(3, shows.Total),
            () => Assert.Equal(2, shows.FilteredTotal),
            () => Assert.Equal(new[] { "a", "b" }, shows.Entries.Select(e => e.StableId).ToArray()),
            () => Assert.Equal(3, all.Total),
            () => Assert.Equal(3, all.FilteredTotal),
            () => Assert.Equal(new[] { "movie", "a", "b" }, all.Entries.Select(e => e.StableId).ToArray()),
            () => Assert.Equal(3, state.Count));
    }

    [Fact]
    public void Query_for_category_without_entries_is_empty()
    {
        var state = With(TestData.Entry("a", _t0));

        var movies = state.Query(new QueryFeed(MediaType.Movie, 0, 10));

        Assert.Multiple(
            () => Assert.Equal(1, movies.Total),
            () => Assert.Equal(0, movies.FilteredTotal),
            () => Assert.Empty(movies.Entries));
    }

    [Fact]
    public void Query_filters_by_channel()
    {
        var state = With(
            TestData.Entry("ard", _t0),
            TestData.Entry("zdf", _t0.AddHours(1)) with { Channel = "ZDF" });

        var result = state.Query(new QueryFeed(null, 0, 10, Channel: "ZDF"));

        Assert.Multiple(
            () => Assert.Equal(2, result.Total),
            () => Assert.Equal(1, result.FilteredTotal),
            () => Assert.Equal("zdf", Assert.Single(result.Entries).StableId));
    }

    [Fact]
    public void Query_filters_by_quality_and_search()
    {
        var state = With(
            TestData.Entry("a", _t0),
            TestData.Entry("b", _t0.AddHours(1)));

        var result = state.Query(new QueryFeed(null, 0, 10, Search: "Tatort"));

        Assert.Multiple(
            () => Assert.Equal(2, result.Total),
            () => Assert.Equal(2, result.FilteredTotal));

        var noMatch = state.Query(new QueryFeed(null, 0, 10, Search: "Nonexistent"));
        Assert.Equal(0, noMatch.FilteredTotal);
    }

    [Fact]
    public void Query_combined_filters_with_offset_beyond_range()
    {
        var state = With(
            TestData.Entry("a", _t0),
            TestData.Entry("b", _t0.AddHours(1)),
            TestData.Entry("c", _t0.AddHours(2)));

        var result = state.Query(new QueryFeed(null, 100, 10, Search: "Tatort"));

        Assert.Multiple(
            () => Assert.Equal(3, result.Total),
            () => Assert.Equal(3, result.FilteredTotal),
            () => Assert.Empty(result.Entries));
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

        var restored = FeedStoreManagerState.FromPersistence(With(original).GetPersistenceState());

        var entry = Assert.Single(QueryAll(restored));
        Assert.Multiple(
            () => Assert.Equal(original.Title, entry.Title),
            () => Assert.Equal(original.Url, entry.Url),
            () => Assert.Equal("2026", entry.Season),
            () => Assert.Equal("18", entry.Episode),
            () => Assert.Equal(original.FirstSeenAt, entry.FirstSeenAt));
    }
}
