using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;

namespace FunkArr.Feed;

internal sealed record FeedCrawlerActors(
    IActorRef Mediathek,
    IActorRef RuleSetResolver,
    IActorRef TvEvaluation,
    IActorRef MovieEvaluation,
    IActorRef FeedManager);

internal sealed class FeedCrawler(FeedCrawlerActors actors, TimeProvider timeProvider)
{
    private const string _sortField = "filmlisteTimestamp";
    private const int _durationMin = 300;

    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _evaluationTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _addTimeout = TimeSpan.FromSeconds(10);

    internal async Task<FeedRunOutcome> RunAsync(long cursor, FeedOptions options, CancellationToken ct)
    {
        var started = timeProvider.GetTimestamp();

        var probe = await QueryAsync(0, 1, ct);
        if (probe is not QueryMediathekCompleted probed)
        {
            return Failed(0, started, Describe(probe));
        }

        var listTimestamp = probed.ListTimestamp;
        if (!FeedIngestPlanner.HasUpdate(cursor, listTimestamp))
        {
            return new FeedRunOutcome(FeedRunStatus.Unchanged, listTimestamp, 0, 0, 0, false, Elapsed(started));
        }

        var window = FeedIngestPlanner.Window(cursor, listTimestamp, options.InitialLookback);
        var pageSize = Math.Max(1, options.PageSize);
        var collected = new List<MediathekItem>();
        var reachedEnd = false;

        for (var page = 0; page < Math.Max(1, options.MaxPages); page++)
        {
            var response = await QueryAsync(page * pageSize, pageSize, ct);
            if (response is not QueryMediathekCompleted completed)
            {
                return Failed(listTimestamp, started, Describe(response));
            }

            var evaluation = FeedIngestPlanner.EvaluatePage(completed.Items, window, pageSize);
            collected.AddRange(evaluation.Collected);

            if (evaluation.ReachedEnd)
            {
                reachedEnd = true;
                break;
            }
        }

        var items = FeedIngestPlanner.Distinct(collected);
        Telemetry.ItemsSeen.Add(items.Length);

        RegisteredRuleSetsResult registered;
        try
        {
            registered = await actors.RuleSetResolver.Ask<RegisteredRuleSetsResult>(
                new QueryRegisteredRuleSets(), _ruleSetTimeout, ct);
        }
        catch (Exception ex)
        {
            return Failed(listTimestamp, started, $"ruleset lookup failed: {ex.Message}");
        }

        var grouping = RuleSetLookup.From(registered.Entries).Group(items);
        Telemetry.Unmatched.Add(grouping.Unmatched);

        var published = 0;
        var failedGroups = 0;

        await Parallel.ForEachAsync(
            grouping.Groups,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.MaxConcurrency), CancellationToken = ct },
            async (group, token) =>
            {
                try
                {
                    var added = await PublishGroupAsync(group, options.MinConfidence, token);
                    Interlocked.Add(ref published, added);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref failedGroups);
                }
            });

        var gap = !reachedEnd;
        if (failedGroups > 0)
        {
            return new FeedRunOutcome(FeedRunStatus.Failed, listTimestamp, items.Length, published, grouping.Unmatched,
                gap, Elapsed(started), $"{failedGroups} of {grouping.Groups.Length} ruleset groups failed");
        }

        return new FeedRunOutcome(FeedRunStatus.Completed, listTimestamp, items.Length, published, grouping.Unmatched,
            gap, Elapsed(started));
    }

    private async Task<int> PublishGroupAsync(RuleSetGroup group, double minConfidence, CancellationToken ct)
    {
        var results = await EvaluateAsync(group, ct);
        var gate = FeedGate.Evaluate(results, group.Items, group.MediaType, minConfidence);

        foreach (var (reason, count) in gate.RejectedByReason)
        {
            Telemetry.Rejected.Add(count, new KeyValuePair<string, object?>("reason", reason));
        }

        if (gate.Accepted.Length == 0)
        {
            return 0;
        }

        var response = await actors.FeedManager.Ask<AddFeedEntriesResponse>(
            new AddFeedEntries(gate.Accepted), _addTimeout, ct);

        return response switch
        {
            AddFeedEntriesCompleted completed => completed.Added,
            AddFeedEntriesFailed failed => throw new InvalidOperationException("Adding feed entries failed", failed.Cause),
            _ => throw new InvalidOperationException($"Unexpected response {response.GetType().Name}")
        };
    }

    private async Task<SearchResultItem[]> EvaluateAsync(RuleSetGroup group, CancellationToken ct)
    {
        var evaluationId = Guid.NewGuid();

        if (group.MediaType == MediaType.Movie)
        {
            var movie = await actors.MovieEvaluation.Ask<EvaluateMoviesResponse>(
                new EvaluateMovies(evaluationId, SearchSource.Feed, group.Items, group.Ids.ImdbId, group.Ids.TmdbId),
                _evaluationTimeout, ct);
            return movie switch
            {
                EvaluateMoviesCompleted completed => completed.Items,
                EvaluateMoviesFailed failed => throw new InvalidOperationException($"Evaluation of {group.RuleSetId} failed", failed.Cause),
                _ => throw new InvalidOperationException($"Unexpected response {movie.GetType().Name}")
            };
        }

        var series = await actors.TvEvaluation.Ask<EvaluateSeriesResponse>(
            new EvaluateSeries(evaluationId, SearchSource.Feed, group.Items, group.Ids.TvdbId, group.Ids.ImdbId),
            _evaluationTimeout, ct);
        return series switch
        {
            EvaluateSeriesCompleted completed => completed.Items,
            EvaluateSeriesFailed failed => throw new InvalidOperationException($"Evaluation of {group.RuleSetId} failed", failed.Cause),
            _ => throw new InvalidOperationException($"Unexpected response {series.GetType().Name}")
        };
    }

    private async Task<QueryMediathekResponse> QueryAsync(int offset, int size, CancellationToken ct)
    {
        var query = new QueryMediathek([], _sortField, "desc", Future: true, offset, size, _durationMin, null);
        try
        {
            return await actors.Mediathek.Ask<QueryMediathekResponse>(query, _mediathekTimeout, ct);
        }
        catch (Exception ex)
        {
            return new QueryMediathekError(ex);
        }
    }

    private static string Describe(QueryMediathekResponse response) => response switch
    {
        QueryMediathekQueueFull => "MediathekViewWeb queue full",
        QueryMediathekError error => $"MediathekViewWeb query failed: {error.Cause.Message}",
        _ => "MediathekViewWeb query failed"
    };

    private FeedRunOutcome Failed(long listTimestamp, long started, string error) =>
        new(FeedRunStatus.Failed, listTimestamp, 0, 0, 0, false, Elapsed(started), error);

    private TimeSpan Elapsed(long started) => timeProvider.GetElapsedTime(started);
}
