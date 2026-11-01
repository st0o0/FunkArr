using FunkArr.Core;

namespace FunkArr.Feed.Tests;

public sealed class FeedCrawlRunTests
{
    private static readonly Guid _runId = Guid.NewGuid();

    private static FeedCrawlRun Planned(bool truncated = false) =>
        FeedCrawlRun.Start(_runId, 0).Planned(FeedCrawlPlan.Create(_runId, 1_000, new CrawlWindow(500, 1_000, truncated),
            RuleSetLookup.Empty, new FeedOptions(), DateTimeOffset.UnixEpoch));

    private static FeedCrawlRun Apply(FeedCrawlRun run, params FeedCrawlEvent[] events) =>
        events.Aggregate(run, (current, evt) => current.Apply(evt));

    [Fact]
    public void Complete_crawl_reports_counts_without_gap()
    {
        var run = Apply(Planned(),
            new FeedPageCrawled(_runId, false), new FeedPageCrawled(_runId, true),
            new FeedItemCollected(_runId), new FeedItemCollected(_runId), new FeedItemCollected(_runId),
            new FeedItemMatched(_runId), new FeedItemMatched(_runId),
            new FeedBatchPublished(_runId, 2, 0, 3));

        var outcome = run.ToOutcome(TimeSpan.FromSeconds(1));

        Assert.Equal(new FeedRunOutcome(FeedRunStatus.Completed, 1_000, 3, 3, 1, false, TimeSpan.FromSeconds(1)), outcome);
    }

    [Fact]
    public void Crawl_without_reaching_the_cursor_is_a_gap()
    {
        var outcome = Apply(Planned(), new FeedPageCrawled(_runId, false)).ToOutcome(TimeSpan.Zero);

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Completed, outcome.Status),
            () => Assert.True(outcome.Gap));
    }

    [Fact]
    public void Truncated_window_is_a_gap()
    {
        var outcome = Apply(Planned(truncated: true), new FeedPageCrawled(_runId, true)).ToOutcome(TimeSpan.Zero);

        Assert.True(outcome.Gap);
    }

    [Fact]
    public void Failed_groups_fail_the_crawl()
    {
        var outcome = Apply(Planned(), new FeedPageCrawled(_runId, true), new FeedBatchPublished(_runId, 3, 1, 2))
            .ToOutcome(TimeSpan.Zero);

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Failed, outcome.Status),
            () => Assert.Equal("1 of 3 ruleset groups failed", outcome.Error),
            () => Assert.Equal(2, outcome.Published));
    }

    [Fact]
    public void Publish_failure_fails_the_crawl()
    {
        var outcome = Apply(Planned(), new FeedPageCrawled(_runId, true), new FeedBatchPublishFailed(_runId, 1, "store down"))
            .ToOutcome(TimeSpan.Zero);

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Failed, outcome.Status),
            () => Assert.Equal("publishing feed entries failed: store down", outcome.Error));
    }

    [Fact]
    public void Stream_failure_keeps_the_list_timestamp()
    {
        var outcome = Apply(Planned(), new FeedCrawlFailed(_runId, "MediathekViewWeb queue full")).ToOutcome(TimeSpan.Zero);

        Assert.Multiple(
            () => Assert.Equal(FeedRunStatus.Failed, outcome.Status),
            () => Assert.Equal(1_000, outcome.ListTimestamp),
            () => Assert.Equal("MediathekViewWeb queue full", outcome.Error));
    }

    [Fact]
    public void Unchanged_reports_the_probed_list_timestamp()
    {
        var outcome = FeedCrawlRun.Start(_runId, 0).Unchanged(900, TimeSpan.Zero);

        Assert.Equal(new FeedRunOutcome(FeedRunStatus.Unchanged, 900, 0, 0, 0, false, TimeSpan.Zero), outcome);
    }
}
