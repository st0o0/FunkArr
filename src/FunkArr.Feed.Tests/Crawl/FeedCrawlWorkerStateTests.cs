using FunkArr.Feed.Crawl;
using FunkArr.Feed.Eval;
using FunkArr.Feed.Page;
using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Tests.Crawl;

public sealed class FeedCrawlWorkerStateTests
{
    private static readonly CrawlInput _input = new(
        Cursor: 1_000_000,
        PageSize: 100,
        MaxPages: 10,
        PagePrefetch: 3,
        MaxConcurrency: 4,
        MinConfidence: 0.7,
        InitialLookback: TimeSpan.FromHours(48),
        MaxCatchUp: TimeSpan.FromDays(7),
        StartedAt: 0);

    private static readonly DateTimeOffset _t0 = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Empty_has_sensible_defaults()
    {
        var state = new FeedCrawlWorkerState(_input);

        Assert.Multiple(
            () => Assert.Equal(0, state.ListTimestamp),
            () => Assert.Null(state.Window),
            () => Assert.Null(state.Lookup),
            () => Assert.Empty(state.CollectedItems),
            () => Assert.Equal(0, state.PendingPages),
            () => Assert.Equal(0, state.NextOffset),
            () => Assert.False(state.ReachedEnd),
            () => Assert.Null(state.PageError),
            () => Assert.Equal(0, state.TotalGroups),
            () => Assert.Equal(0, state.PendingGroups),
            () => Assert.Equal(0, state.FailedGroups),
            () => Assert.Empty(state.AcceptedEntries),
            () => Assert.Null(state.FirstGroupError));
    }

    [Fact]
    public void ForInput_sets_input_and_resets_to_empty()
    {
        var state = new FeedCrawlWorkerState(_input);

        Assert.Multiple(
            () => Assert.Equal(_input, state.Input),
            () => Assert.Equal(0, state.ListTimestamp),
            () => Assert.Empty(state.CollectedItems));
    }

    [Fact]
    public void ApplyProbe_sets_list_timestamp()
    {
        var state = new FeedCrawlWorkerState(_input);
        state.ListTimestamp = 2_000_000;

        Assert.Equal(2_000_000, state.ListTimestamp);
    }

    [Fact]
    public void ApplyRuleSets_sets_lookup_and_window()
    {
        var lookup = RuleSetLookup.Empty;
        var window = new CrawlWindow(1_000_000, 2_000_000);

        var state = new FeedCrawlWorkerState(_input);
        state.ListTimestamp = 2_000_000;
        state.Lookup = lookup;
        state.Window = window;

        Assert.Multiple(
            () => Assert.Same(lookup, state.Lookup),
            () => Assert.Equal(window, state.Window));
    }

    [Fact]
    public void StartHarvesting_sets_pending_pages_and_next_offset()
    {
        var state = new FeedCrawlWorkerState(_input);
        state.StartHarvesting(3);

        Assert.Multiple(
            () => Assert.Equal(3, state.PendingPages),
            () => Assert.Equal(300, state.NextOffset));
    }

    [Fact]
    public void ApplyPageResult_collects_items_and_decrements_pending()
    {
        var items = new[] { TestData.Item(), TestData.Item(title: "Zweite") };
        var page = new PageResult(0, items, false, null);

        var state = new FeedCrawlWorkerState(_input);
        state.StartHarvesting(2);
        state.Apply(page);

        Assert.Multiple(
            () => Assert.Equal(2, state.CollectedItems.Count),
            () => Assert.Equal(1, state.PendingPages),
            () => Assert.False(state.ReachedEnd),
            () => Assert.Null(state.PageError));
    }

    [Fact]
    public void ApplyPageResult_sets_reached_end_and_preserves_it()
    {
        var endPage = new PageResult(0, [TestData.Item()], true, null);
        var normalPage = new PageResult(100, [TestData.Item(title: "Zweite")], false, null);

        var state = new FeedCrawlWorkerState(_input);
        state.StartHarvesting(2);
        state.Apply(endPage);
        state.Apply(normalPage);

        Assert.True(state.ReachedEnd);
    }

    [Fact]
    public void ApplyPageResult_captures_first_error()
    {
        var errorPage = new PageResult(0, [], true, "connection failed");
        var anotherError = new PageResult(100, [], true, "timeout");

        var state = new FeedCrawlWorkerState(_input);
        state.StartHarvesting(2);
        state.Apply(errorPage);
        state.Apply(anotherError);

        Assert.Equal("connection failed", state.PageError);
    }

    [Fact]
    public void FireNextPage_increments_pending_and_offset()
    {
        var state = new FeedCrawlWorkerState(_input);
        state.StartHarvesting(2);
        state.FireNextPage();

        Assert.Multiple(
            () => Assert.Equal(3, state.PendingPages),
            () => Assert.Equal(300, state.NextOffset));
    }

    [Fact]
    public void StartEvaluating_sets_total_and_pending_groups()
    {
        var state = new FeedCrawlWorkerState(_input);
        state.StartEvaluating(5);

        Assert.Multiple(
            () => Assert.Equal(5, state.TotalGroups),
            () => Assert.Equal(5, state.PendingGroups));
    }

    [Fact]
    public void ApplyGroupResult_collects_entries_and_decrements_pending()
    {
        var entries = new[] { TestData.Entry("a", _t0), TestData.Entry("b", _t0) };
        var result = new GroupResult("rs-1", entries, null, null);

        var state = new FeedCrawlWorkerState(_input);
        state.StartEvaluating(2);
        state.Apply(result);

        Assert.Multiple(
            () => Assert.Equal(2, state.AcceptedEntries.Count),
            () => Assert.Equal(1, state.PendingGroups),
            () => Assert.Equal(0, state.FailedGroups),
            () => Assert.Null(state.FirstGroupError));
    }

    [Fact]
    public void ApplyGroupResult_tracks_failures_and_first_error()
    {
        var ok = new GroupResult("rs-1", [TestData.Entry("a", _t0)], null, null);
        var failed = new GroupResult("rs-2", [], null, "eval timeout");

        var state = new FeedCrawlWorkerState(_input);
        state.StartEvaluating(2);
        state.Apply(ok);
        state.Apply(failed);

        Assert.Multiple(
            () => Assert.Equal(0, state.PendingGroups),
            () => Assert.Equal(1, state.FailedGroups),
            () => Assert.Equal("eval timeout", state.FirstGroupError));
    }

    [Fact]
    public void GetCrawlResult_completed_when_no_errors()
    {
        var window = new CrawlWindow(1_000_000, 2_000_000);
        var state = new FeedCrawlWorkerState(_input);
        state.ListTimestamp = 2_000_000;
        state.Lookup = RuleSetLookup.Empty;
        state.Window = window;
        state.StartHarvesting(1);
        state.Apply(new PageResult(0, [TestData.Item()], true, null));
        state.StartEvaluating(1);
        state.Apply(new GroupResult("rs-1", [TestData.Entry("a", _t0)], null, null));

        var result = state.GetCrawlResult(TimeSpan.FromSeconds(5));

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, result.Status),
            () => Assert.Equal(2_000_000, result.ListTimestamp),
            () => Assert.Equal(1, result.NewItems),
            () => Assert.Equal(1, result.Published),
            () => Assert.Null(result.Error));
    }

    [Fact]
    public void GetCrawlResult_failed_on_page_error_with_zero_items()
    {
        var state = new FeedCrawlWorkerState(_input);
        state.ListTimestamp = 2_000_000;
        state.Lookup = RuleSetLookup.Empty;
        state.Window = new CrawlWindow(1_000_000, 2_000_000);
        state.StartHarvesting(1);
        state.Apply(new PageResult(0, [], true, "network error"));

        var result = state.GetCrawlResult(TimeSpan.FromSeconds(1));

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Failed, result.Status),
            () => Assert.Equal("network error", result.Error),
            () => Assert.Equal(0, result.NewItems),
            () => Assert.Equal(0, result.Unmatched));
    }

    [Fact]
    public void GetCrawlResult_completed_partial_on_page_error_with_collected_items()
    {
        var state = new FeedCrawlWorkerState(_input);
        state.ListTimestamp = 2_000_000;
        state.Lookup = RuleSetLookup.Empty;
        state.Window = new CrawlWindow(1_000_000, 2_000_000);
        state.StartHarvesting(2);
        state.Apply(new PageResult(0, [TestData.Item()], false, null));
        state.Apply(new PageResult(100, [], true, "server error"));
        state.StartEvaluating(1);
        state.Apply(new GroupResult("rs-1", [TestData.Entry("a", _t0)], null, null));

        var result = state.GetCrawlResult(TimeSpan.FromSeconds(3));

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.CompletedWithGap, result.Status),
            () => Assert.True(result.Gap),
            () => Assert.Equal(1, result.NewItems),
            () => Assert.Equal(1, result.Published),
            () => Assert.Null(result.Error));
    }

    [Fact]
    public void GetCrawlResult_completed_partial_regardless_of_page_arrival_order()
    {
        var successPage = new PageResult(0, [TestData.Item()], false, null);
        var errorPage = new PageResult(100, [], true, "server error");

        var stateErrorLast = new FeedCrawlWorkerState(_input);
        stateErrorLast.ListTimestamp = 2_000_000;
        stateErrorLast.Lookup = RuleSetLookup.Empty;
        stateErrorLast.Window = new CrawlWindow(1_000_000, 2_000_000);
        stateErrorLast.StartHarvesting(2);
        stateErrorLast.Apply(successPage);
        stateErrorLast.Apply(errorPage);
        stateErrorLast.StartEvaluating(1);
        stateErrorLast.Apply(new GroupResult("rs-1", [TestData.Entry("a", _t0)], null, null));

        var stateErrorFirst = new FeedCrawlWorkerState(_input);
        stateErrorFirst.ListTimestamp = 2_000_000;
        stateErrorFirst.Lookup = RuleSetLookup.Empty;
        stateErrorFirst.Window = new CrawlWindow(1_000_000, 2_000_000);
        stateErrorFirst.StartHarvesting(2);
        stateErrorFirst.Apply(errorPage);
        stateErrorFirst.Apply(successPage);
        stateErrorFirst.StartEvaluating(1);
        stateErrorFirst.Apply(new GroupResult("rs-1", [TestData.Entry("a", _t0)], null, null));

        var resultErrorLast = stateErrorLast.GetCrawlResult(TimeSpan.FromSeconds(1));
        var resultErrorFirst = stateErrorFirst.GetCrawlResult(TimeSpan.FromSeconds(1));

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.CompletedWithGap, resultErrorLast.Status),
            () => Assert.Equal(CrawlOutcome.CompletedWithGap, resultErrorFirst.Status),
            () => Assert.Equal(resultErrorLast.Published, resultErrorFirst.Published),
            () => Assert.Equal(resultErrorLast.NewItems, resultErrorFirst.NewItems));
    }

    [Fact]
    public void GetCrawlResult_failed_on_group_failures()
    {
        var state = new FeedCrawlWorkerState(_input);
        state.ListTimestamp = 2_000_000;
        state.Lookup = RuleSetLookup.Empty;
        state.Window = new CrawlWindow(1_000_000, 2_000_000);
        state.StartHarvesting(1);
        state.Apply(new PageResult(0, [TestData.Item()], true, null));
        state.StartEvaluating(3);
        state.Apply(new GroupResult("rs-1", [], null, "timeout"));
        state.Apply(new GroupResult("rs-2", [], null, "timeout"));
        state.Apply(new GroupResult("rs-3", [TestData.Entry("a", _t0)], null, null));

        var result = state.GetCrawlResult(TimeSpan.FromSeconds(2));

        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Failed, result.Status),
            () => Assert.Equal("2 of 3 ruleset groups failed", result.Error));
    }
}
