using FunkArr.Messages.Feed;

namespace FunkArr.Feed;

internal abstract record CrawlStep;

internal sealed record CrawlProbed(long ListTimestamp, bool HasUpdate, bool Truncated) : CrawlStep;

internal sealed record PageCollected(int Items, int Unmatched, bool ReachedEnd) : CrawlStep;

internal sealed record GroupPending(RuleSetGroup Group) : CrawlStep;

internal sealed record GroupEvaluated(string RuleSetId, FeedEntry[] Accepted) : CrawlStep;

internal sealed record GroupFailed(string RuleSetId, string Error) : CrawlStep;

internal sealed record EntriesPublished(int Count) : CrawlStep;

internal sealed record PublishFailed(string Error) : CrawlStep;

internal sealed record CrawlFailed(string Error) : CrawlStep;

internal sealed record FeedRunStats(
    long ListTimestamp,
    bool HasUpdate,
    bool Truncated,
    int NewItems,
    int Unmatched,
    bool ReachedEnd,
    int Groups,
    int FailedGroups,
    int Published,
    string? Error)
{
    internal static readonly FeedRunStats Empty = new(0, false, false, 0, 0, false, 0, 0, 0, null);

    internal FeedRunStats Add(CrawlStep step) => step switch
    {
        CrawlProbed probed => this with { ListTimestamp = probed.ListTimestamp, HasUpdate = probed.HasUpdate, Truncated = probed.Truncated },
        PageCollected page => this with { NewItems = NewItems + page.Items, Unmatched = Unmatched + page.Unmatched, ReachedEnd = ReachedEnd || page.ReachedEnd },
        GroupEvaluated => this with { Groups = Groups + 1 },
        GroupFailed => this with { Groups = Groups + 1, FailedGroups = FailedGroups + 1 },
        EntriesPublished published => this with { Published = Published + published.Count },
        PublishFailed failed => this with { Error = Error ?? $"publishing feed entries failed: {failed.Error}" },
        CrawlFailed failed => this with { Error = Error ?? failed.Error },
        _ => this
    };

    internal FeedRunOutcome ToOutcome(TimeSpan duration)
    {
        if (Error is not null)
        {
            return new FeedRunOutcome(FeedRunStatus.Failed, ListTimestamp, NewItems, Published, Unmatched, false, duration, Error);
        }

        if (!HasUpdate)
        {
            return new FeedRunOutcome(FeedRunStatus.Unchanged, ListTimestamp, 0, 0, 0, false, duration);
        }

        var gap = Truncated || !ReachedEnd;
        return FailedGroups > 0
            ? new FeedRunOutcome(FeedRunStatus.Failed, ListTimestamp, NewItems, Published, Unmatched, gap, duration,
                $"{FailedGroups} of {Groups} ruleset groups failed")
            : new FeedRunOutcome(FeedRunStatus.Completed, ListTimestamp, NewItems, Published, Unmatched, gap, duration);
    }
}
