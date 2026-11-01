using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;

namespace FunkArr.Feed;

internal sealed record FeedCrawlPlan(
    Guid RunId,
    long ListTimestamp,
    CrawlWindow Window,
    RuleSetLookup Lookup,
    int PageSize,
    int MaxPages,
    double MinConfidence,
    long LeadLimit)
{
    private const string _sortField = "filmlisteTimestamp";
    private const int _durationMin = 300;

    internal static FeedCrawlPlan Create(Guid runId, long listTimestamp, CrawlWindow window, RuleSetLookup lookup,
        FeedOptions options, DateTimeOffset now) =>
        new(runId, listTimestamp, window, lookup, Math.Max(1, options.PageSize), Math.Max(1, options.MaxPages),
            options.MinConfidence, (now + FeedCrawlPlanner.MaxLead).ToUnixTimeSeconds());

    internal static QueryMediathek Query(int offset, int size) =>
        new([], _sortField, "desc", Future: true, offset, size, _durationMin, null);

    internal IEnumerable<int> PageOffsets => Enumerable.Range(0, MaxPages).Select(page => page * PageSize);

    internal QueryMediathek PageQuery(int offset) => Query(offset, PageSize);

    internal PageEvaluation Read(QueryMediathekResponse response) => response switch
    {
        QueryMediathekCompleted completed => FeedCrawlPlanner.EvaluatePage(completed.Items, Window, PageSize),
        QueryMediathekQueueFull => throw new InvalidOperationException("MediathekViewWeb queue full"),
        QueryMediathekError error => throw new InvalidOperationException($"MediathekViewWeb query failed: {error.Cause.Message}", error.Cause),
        _ => throw new InvalidOperationException($"Unexpected response {response.GetType().Name}")
    };

    internal bool WithinLead(MediathekItem item) => item.Timestamp <= LeadLimit;

    internal IEnumerable<FeedMatch> Match(MediathekItem item) =>
        Lookup.Match(item.Topic) is { } target ? [new FeedMatch(item, target)] : [];

    internal static RuleSetGroup Group(IEnumerable<FeedMatch> matches)
    {
        var list = matches.ToArray();
        var target = list[0].Target;
        return new RuleSetGroup(target.RuleSetId, target.MediaType, target.Ids, [.. list.Select(m => m.Item)]);
    }

    internal static EvaluateSeries EvaluateSeries(RuleSetGroup group) =>
        new(Guid.NewGuid(), SearchSource.Feed, group.Items, group.Ids.TvdbId, group.Ids.ImdbId);

    internal static EvaluateMovies EvaluateMovies(RuleSetGroup group) =>
        new(Guid.NewGuid(), SearchSource.Feed, group.Items, group.Ids.ImdbId, group.Ids.TmdbId);
}

internal sealed record FeedMatch(MediathekItem Item, RuleSetTarget Target);

internal sealed record FeedEvaluation(RuleSetGroup Group, SearchResultItem[] Results, string? Error)
{
    internal static FeedEvaluation Of(RuleSetGroup group, SearchResultItem[] results) => new(group, results, null);

    internal static FeedEvaluation Failed(RuleSetGroup group, Exception cause) => new(group, [], cause.Message);

    internal FeedGated Gate(double minConfidence)
    {
        if (Error is not null)
        {
            return new FeedGated(Group.RuleSetId, [], Error);
        }

        var gate = FeedGate.Evaluate(Results, Group.Items, Group.MediaType, minConfidence);
        foreach (var (reason, count) in gate.RejectedByReason)
        {
            Telemetry.Rejected.Add(count, new KeyValuePair<string, object?>("reason", reason));
        }

        return new FeedGated(Group.RuleSetId, gate.Accepted, null);
    }
}

internal sealed record FeedGated(string RuleSetId, FeedEntry[] Accepted, string? Error);

internal static class FeedCrawlResponses
{
    internal static SearchResultItem[] ItemsOrThrow(this EvaluateSeriesResponse response) => response switch
    {
        EvaluateSeriesCompleted completed => completed.Items,
        EvaluateSeriesFailed failed => throw new InvalidOperationException("Series evaluation failed", failed.Cause),
        _ => throw new InvalidOperationException($"Unexpected response {response.GetType().Name}")
    };

    internal static SearchResultItem[] ItemsOrThrow(this EvaluateMoviesResponse response) => response switch
    {
        EvaluateMoviesCompleted completed => completed.Items,
        EvaluateMoviesFailed failed => throw new InvalidOperationException("Movie evaluation failed", failed.Cause),
        _ => throw new InvalidOperationException($"Unexpected response {response.GetType().Name}")
    };

    internal static int PublishedOrThrow(this PublishFeedEntriesResponse response) => response switch
    {
        PublishFeedEntriesCompleted completed => completed.Published,
        PublishFeedEntriesFailed failed => throw new InvalidOperationException("Publishing feed entries failed", failed.Cause),
        _ => throw new InvalidOperationException($"Unexpected response {response.GetType().Name}")
    };
}
