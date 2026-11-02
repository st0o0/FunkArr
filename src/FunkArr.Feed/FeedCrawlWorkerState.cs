using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Feed;

internal sealed record FeedCrawlWorkerState(
    CrawlInput Input,
    long ListTimestamp,
    CrawlWindow? Window,
    RuleSetLookup? Lookup,
    List<MediathekItem> CollectedItems,
    int PendingPages,
    int NextOffset,
    bool ReachedEnd,
    string? PageError,
    int TotalGroups,
    int PendingGroups,
    int FailedGroups,
    List<FeedEntry> AcceptedEntries,
    string? FirstGroupError)
{
    internal static readonly FeedCrawlWorkerState Empty = new(
        default!, 0, null, null,
        [], 0, 0, false, null,
        0, 0, 0, [], null);
}

internal static class FeedCrawlWorkerStateExtensions
{
    public static FeedCrawlWorkerState ForInput(this FeedCrawlWorkerState _, CrawlInput input) =>
        new(input, 0, null, null, [], 0, 0, false, null, 0, 0, 0, [], null);

    public static FeedCrawlWorkerState ApplyProbe(this FeedCrawlWorkerState state, long listTimestamp) =>
        state with { ListTimestamp = listTimestamp };

    public static FeedCrawlWorkerState ApplyRuleSets(
        this FeedCrawlWorkerState state, RuleSetLookup lookup, CrawlWindow window) =>
        state with { Lookup = lookup, Window = window };

    public static FeedCrawlWorkerState StartHarvesting(
        this FeedCrawlWorkerState state, int initialRequests) =>
        state with
        {
            PendingPages = initialRequests,
            NextOffset = initialRequests * state.Input.PageSize
        };

    public static FeedCrawlWorkerState ApplyPageResult(
        this FeedCrawlWorkerState state, PageResult page)
    {
        state.CollectedItems.AddRange(page.Items);

        return state with
        {
            PendingPages = state.PendingPages - 1,
            ReachedEnd = state.ReachedEnd || page.ReachedEnd,
            PageError = state.PageError ?? page.Error
        };
    }

    public static FeedCrawlWorkerState FireNextPage(this FeedCrawlWorkerState state) =>
        state with
        {
            PendingPages = state.PendingPages + 1,
            NextOffset = state.NextOffset + state.Input.PageSize
        };

    public static FeedCrawlWorkerState StartEvaluating(
        this FeedCrawlWorkerState state, int totalGroups) =>
        state with { TotalGroups = totalGroups, PendingGroups = totalGroups };

    public static FeedCrawlWorkerState ApplyGroupResult(
        this FeedCrawlWorkerState state, GroupResult result)
    {
        state.AcceptedEntries.AddRange(result.Accepted);

        return state with
        {
            PendingGroups = state.PendingGroups - 1,
            FailedGroups = state.FailedGroups + (result.Error is not null ? 1 : 0),
            FirstGroupError = state.FirstGroupError ?? result.Error
        };
    }

    public static CrawlResult GetCrawlResult(
        this FeedCrawlWorkerState state, TimeSpan duration)
    {
        var collected = state.CollectedItems.Count;
        var matched = state.TotalGroups;
        var unmatched = collected - matched;
        var published = state.AcceptedEntries.Count;
        var truncated = state.Window?.Truncated ?? false;

        if (state.PageError is not null)
        {
            return CrawlResult.ForFailed(state.ListTimestamp, collected, unmatched, published,
                truncated, duration, state.PageError);
        }

        if (state.FailedGroups > 0)
        {
            return CrawlResult.ForFailed(state.ListTimestamp, collected, unmatched, published,
                truncated, duration,
                $"{state.FailedGroups} of {state.TotalGroups} ruleset groups failed");
        }

        if (state.FirstGroupError is not null)
        {
            return CrawlResult.ForFailed(state.ListTimestamp, collected, unmatched, published,
                truncated, duration, state.FirstGroupError);
        }

        return CrawlResult.ForCompleted(state.ListTimestamp, collected, unmatched, published,
            truncated, duration);
    }
}
