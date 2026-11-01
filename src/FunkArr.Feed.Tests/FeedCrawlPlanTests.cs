using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Tests;

public sealed class FeedCrawlPlanTests
{
    private static readonly DateTimeOffset _now = DateTimeOffset.FromUnixTimeSeconds(1_761_900_000);

    private static FeedCrawlPlan Plan() => FeedCrawlPlan.Create(
        Guid.NewGuid(), 1_761_900_000, new CrawlWindow(1_761_800_000, 1_761_900_000),
        RuleSetLookup.From([new RegisteredRuleSetEntry("tatort", "Tatort", ["Tatort (ARD)"], new ExternalIds(76290, null, null), "Tatort", MediaType.Show)]),
        new FeedOptions { PageSize = 100, MaxPages = 3 }, _now);

    [Fact]
    public void Page_offsets_follow_page_size_and_limit()
    {
        Assert.Equal(new[] { 0, 100, 200 }, Plan().PageOffsets.ToArray());
    }

    [Fact]
    public void Page_query_sorts_by_filmliste_timestamp_and_includes_future_entries()
    {
        var query = Plan().PageQuery(200);

        Assert.Multiple(
            () => Assert.Equal("filmlisteTimestamp", query.SortBy),
            () => Assert.Equal("desc", query.SortOrder),
            () => Assert.True(query.Future),
            () => Assert.Equal(200, query.Offset),
            () => Assert.Equal(100, query.Size),
            () => Assert.Equal(300, query.DurationMin));
    }

    [Fact]
    public void Read_rejects_failed_queries()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Plan().Read(new QueryMediathekError(new Exception("HTTP 500"))));

        Assert.Equal("MediathekViewWeb query failed: HTTP 500", ex.Message);
    }

    [Fact]
    public void Match_returns_the_ruleset_by_alias()
    {
        var match = Assert.Single(Plan().Match(TestData.Item("a", "Tatort (ARD)")));

        Assert.Multiple(
            () => Assert.Equal("tatort", match.Target.RuleSetId),
            () => Assert.Equal(76290, match.Target.Ids.TvdbId));
    }

    [Fact]
    public void Match_drops_entries_without_ruleset()
    {
        Assert.Empty(Plan().Match(TestData.Item("a", "Unbekannt")));
    }

    [Fact]
    public void Group_combines_matches_of_one_ruleset()
    {
        var plan = Plan();
        var matches = plan.Match(TestData.Item("a")).Concat(plan.Match(TestData.Item("b", "Tatort (ARD)")));

        var group = FeedCrawlPlan.Group(matches);

        Assert.Multiple(
            () => Assert.Equal("tatort", group.RuleSetId),
            () => Assert.Equal(MediaType.Show, group.MediaType),
            () => Assert.Equal(new[] { "a", "b" }, group.Items.Select(i => i.Title).ToArray()));
    }

    [Fact]
    public void WithinLead_rejects_entries_more_than_90_days_ahead()
    {
        var plan = Plan();

        Assert.Multiple(
            () => Assert.True(plan.WithinLead(TestData.Item("a") with { Timestamp = _now.AddDays(89).ToUnixTimeSeconds() })),
            () => Assert.False(plan.WithinLead(TestData.Item("b") with { Timestamp = _now.AddDays(91).ToUnixTimeSeconds() })));
    }
}
