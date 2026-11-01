using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;

namespace FunkArr.Feed;

internal sealed record FeedCrawlStages(
    Func<int, int, CancellationToken, Task<QueryMediathekResponse>> QueryPage,
    Func<CancellationToken, Task<RegisteredRuleSetEntry[]>> LoadRuleSets,
    Func<RuleSetGroup, CancellationToken, Task<SearchResultItem[]>> Evaluate,
    Func<FeedEntry[], CancellationToken, Task<int>> Publish)
{
    private const string _sortField = "filmlisteTimestamp";
    private const int _durationMin = 300;

    private static readonly TimeSpan _mediathekTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _ruleSetTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _evaluationTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _publishTimeout = TimeSpan.FromSeconds(10);

    internal static FeedCrawlStages FromActors(
        IActorRef mediathek,
        IActorRef ruleSetResolver,
        IActorRef tvEvaluation,
        IActorRef movieEvaluation,
        IActorRef store) =>
        new(
            QueryPage: (offset, size, ct) => mediathek.Ask<QueryMediathekResponse>(
                new QueryMediathek([], _sortField, "desc", Future: true, offset, size, _durationMin, null), _mediathekTimeout, ct),
            LoadRuleSets: async ct =>
                (await ruleSetResolver.Ask<RegisteredRuleSetsResult>(new QueryRegisteredRuleSets(), _ruleSetTimeout, ct)).Entries,
            Evaluate: (group, ct) => group.MediaType == MediaType.Movie
                ? EvaluateMovies(movieEvaluation, group, ct)
                : EvaluateSeries(tvEvaluation, group, ct),
            Publish: async (entries, ct) =>
                (await store.Ask<PublishFeedEntriesResponse>(new PublishFeedEntries(entries), _publishTimeout, ct)) switch
                {
                    PublishFeedEntriesCompleted completed => completed.Published,
                    PublishFeedEntriesFailed failed => throw new InvalidOperationException("Publishing feed entries failed", failed.Cause),
                    var other => throw new InvalidOperationException($"Unexpected response {other.GetType().Name}")
                });

    private static async Task<SearchResultItem[]> EvaluateSeries(IActorRef region, RuleSetGroup group, CancellationToken ct) =>
        (await region.Ask<EvaluateSeriesResponse>(
                new EvaluateSeries(Guid.NewGuid(), SearchSource.Feed, group.Items, group.Ids.TvdbId, group.Ids.ImdbId),
                _evaluationTimeout, ct)) switch
        {
            EvaluateSeriesCompleted completed => completed.Items,
            EvaluateSeriesFailed failed => throw new InvalidOperationException($"Evaluation of {group.RuleSetId} failed", failed.Cause),
            var other => throw new InvalidOperationException($"Unexpected response {other.GetType().Name}")
        };

    private static async Task<SearchResultItem[]> EvaluateMovies(IActorRef region, RuleSetGroup group, CancellationToken ct) =>
        (await region.Ask<EvaluateMoviesResponse>(
                new EvaluateMovies(Guid.NewGuid(), SearchSource.Feed, group.Items, group.Ids.ImdbId, group.Ids.TmdbId),
                _evaluationTimeout, ct)) switch
        {
            EvaluateMoviesCompleted completed => completed.Items,
            EvaluateMoviesFailed failed => throw new InvalidOperationException($"Evaluation of {group.RuleSetId} failed", failed.Cause),
            var other => throw new InvalidOperationException($"Unexpected response {other.GetType().Name}")
        };
}
