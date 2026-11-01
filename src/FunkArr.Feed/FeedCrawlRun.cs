namespace FunkArr.Feed;

internal abstract record FeedCrawlEvent(Guid RunId);

internal sealed record FeedPageCrawled(Guid RunId, bool ReachedEnd) : FeedCrawlEvent(RunId);

internal sealed record FeedItemCollected(Guid RunId) : FeedCrawlEvent(RunId);

internal sealed record FeedItemMatched(Guid RunId) : FeedCrawlEvent(RunId);

internal sealed record FeedBatchPublished(Guid RunId, int Groups, int FailedGroups, int Published) : FeedCrawlEvent(RunId);

internal sealed record FeedBatchPublishFailed(Guid RunId, int Groups, string Error) : FeedCrawlEvent(RunId);

internal sealed record FeedCrawlFinished(Guid RunId) : FeedCrawlEvent(RunId);

internal sealed record FeedCrawlFailed(Guid RunId, string Error) : FeedCrawlEvent(RunId);

internal sealed record FeedCrawlRun(
    Guid RunId,
    long StartedAt,
    long ListTimestamp,
    bool Truncated,
    bool ReachedEnd,
    int Collected,
    int Matched,
    int Groups,
    int FailedGroups,
    int Published,
    string? Error)
{
    internal static FeedCrawlRun Start(Guid runId, long startedAt) =>
        new(runId, startedAt, 0, false, false, 0, 0, 0, 0, 0, null);
}

internal static class FeedCrawlRunExtensions
{
    internal static FeedCrawlRun Planned(this FeedCrawlRun run, FeedCrawlPlan plan) =>
        run with { ListTimestamp = plan.ListTimestamp, Truncated = plan.Window.Truncated };

    internal static FeedCrawlRun Apply(this FeedCrawlRun run, FeedCrawlEvent evt) => evt switch
    {
        FeedPageCrawled page => run with { ReachedEnd = run.ReachedEnd || page.ReachedEnd },
        FeedItemCollected => run with { Collected = run.Collected + 1 },
        FeedItemMatched => run with { Matched = run.Matched + 1 },
        FeedBatchPublished batch => run with
        {
            Groups = run.Groups + batch.Groups,
            FailedGroups = run.FailedGroups + batch.FailedGroups,
            Published = run.Published + batch.Published
        },
        FeedBatchPublishFailed failed => run with
        {
            Groups = run.Groups + failed.Groups,
            Error = run.Error ?? $"publishing feed entries failed: {failed.Error}"
        },
        FeedCrawlFailed failed => run with { Error = run.Error ?? failed.Error },
        _ => run
    };

    internal static FeedRunOutcome Unchanged(this FeedCrawlRun run, long listTimestamp, TimeSpan duration) =>
        new(FeedRunStatus.Unchanged, listTimestamp, 0, 0, 0, false, duration);

    internal static FeedRunOutcome Failed(this FeedCrawlRun run, string error, TimeSpan duration) =>
        new(FeedRunStatus.Failed, run.ListTimestamp, run.Collected, run.Published, run.Collected - run.Matched, false, duration, error);

    internal static FeedRunOutcome ToOutcome(this FeedCrawlRun run, TimeSpan duration)
    {
        if (run.Error is not null)
        {
            return run.Failed(run.Error, duration);
        }

        var unmatched = run.Collected - run.Matched;
        var gap = run.Truncated || !run.ReachedEnd;
        return run.FailedGroups > 0
            ? new FeedRunOutcome(FeedRunStatus.Failed, run.ListTimestamp, run.Collected, run.Published, unmatched, gap, duration,
                $"{run.FailedGroups} of {run.Groups} ruleset groups failed")
            : new FeedRunOutcome(FeedRunStatus.Completed, run.ListTimestamp, run.Collected, run.Published, unmatched, gap, duration);
    }
}
