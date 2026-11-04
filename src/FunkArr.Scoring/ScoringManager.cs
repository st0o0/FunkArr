using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Microsoft.Extensions.Options;
using Terp.Akka;
using Terp.Diagnostics;

namespace FunkArr.Scoring;

public sealed class ScoringManager : ReceiveActor
{
    private static readonly TimeSpan _enrichmentTimeout = TimeSpan.FromSeconds(10);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _router;
    private ScoringManagerState _state = ScoringManagerState.Empty;

    public ScoringManager(IOptionsMonitor<ScoringOptions> optionsMonitor)
    {
        var routerProps = Props.Create<ScoringActor>()
            .WithRouter(new SmallestMailboxPool(optionsMonitor.CurrentValue.PoolSize));
        _router = Context.ActorOf(routerProps, "scoring-pool");

        Receive<MatchingConfig>(config => _state = _state.Apply(config));
        Receive<RemoveMatchingConfig>(msg => _state = _state.Apply(msg));
        Receive<ScoreItems>(HandleScoreItems);
        Receive<TestScoreItems>(HandleTestScoreItems);
        ReceiveAsync<TestScoreWithEnrichment>(HandleTestScoreWithEnrichment);
    }

    private void HandleScoreItems(ScoreItems msg)
    {
        var config = _state.GetConfig(msg.RuleSetId);
        if (config is null)
        {
            Telemetry.NoConfig.Up();
            _log.Debug("No matching config for ruleset {RuleSetId}, returning unscored defaults", msg.RuleSetId);
            var defaults = msg.Candidates.Select((_, i) => new ScoredItem(i, 0.0, false)).ToArray();
            Sender.Tell(new ScoreCompleted(msg.RequestId, defaults, []));
            return;
        }

        _router.Tell(new ExecuteScoring(config, msg.Candidates, msg.RequestId), Sender);
    }

    private void HandleTestScoreItems(TestScoreItems msg)
        => _router.Tell(new ExecuteScoring(msg.Config, msg.Candidates, msg.RequestId), Sender);

    private async Task HandleTestScoreWithEnrichment(TestScoreWithEnrichment msg)
    {
        var scoreResult = await _router.Ask<ScoreItemsResponse>(
            new ExecuteScoring(msg.Config, msg.Candidates, msg.RequestId), _enrichmentTimeout);

        if (scoreResult is not ScoreCompleted completed)
        {
            Sender.Tell(new TestScoreWithEnrichmentFailed(
                new InvalidOperationException("Scoring failed")));
            return;
        }

        var itemTraces = completed.ItemTraces;

        if (msg.EnrichmentConfig is not null)
        {
            itemTraces = await RunEnrichment(itemTraces, msg);
        }

        Sender.Tell(new TestScoreWithEnrichmentCompleted(itemTraces));
    }

    private async Task<ItemTrace[]> RunEnrichment(ItemTrace[] itemTraces, TestScoreWithEnrichment msg)
    {
        var isShow = msg.MediaType == Messages.MediaType.Show;
        var isMovie = msg.MediaType == Messages.MediaType.Movie;

        try
        {
            var enrichmentManager = Context.GetActor<IEnrichmentManager>();

            if (isShow && msg.TvdbId is not null)
                return await EnrichEpisodes(itemTraces, msg, enrichmentManager);

            if (isMovie && (msg.TmdbId is not null || msg.ImdbId is not null))
                return await EnrichMovies(itemTraces, msg, enrichmentManager);
        }
        catch
        {
            // enrichment timeout or failure: return scoring results without enrichment
        }

        return itemTraces;
    }

    private async Task<ItemTrace[]> EnrichEpisodes(
        ItemTrace[] itemTraces, TestScoreWithEnrichment msg, IActorRef enrichmentManager)
    {
        var matchedIndices = new List<(int TraceIndex, EpisodeCandidate Candidate)>();

        for (var i = 0; i < itemTraces.Length; i++)
        {
            var trace = itemTraces[i];
            if (!trace.Matched)
                continue;

            var airedAt = trace.Candidate.Timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(trace.Candidate.Timestamp)
                : (DateTimeOffset?)null;

            matchedIndices.Add((i, new EpisodeCandidate(
                matchedIndices.Count,
                trace.Candidate.Title,
                trace.Identification?.Title,
                airedAt,
                trace.Candidate.Duration,
                trace.Identification?.Season,
                trace.Identification?.Episode)));
        }

        if (matchedIndices.Count == 0)
            return itemTraces;

        var enrichRequest = new EnrichEpisodes(
            msg.TvdbId!.Value, Season: null,
            [.. matchedIndices.Select(m => m.Candidate)], msg.EnrichmentConfig!);

        var response = await enrichmentManager.Ask<EnrichEpisodesResponse>(enrichRequest, _enrichmentTimeout);
        if (response is not EnrichEpisodesCompleted completed)
            return itemTraces;

        var enrichedLookup = completed.Episodes.ToDictionary(e => e.Index);
        var result = new ItemTrace[itemTraces.Length];
        Array.Copy(itemTraces, result, itemTraces.Length);

        foreach (var (traceIndex, candidate) in matchedIndices)
        {
            var trace = result[traceIndex];
            if (enrichedLookup.TryGetValue(candidate.Index, out var enriched))
            {
                result[traceIndex] = trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        enriched.Method, enriched.Confidence, true,
                        enriched.Season, enriched.Episode, enriched.EpisodeName)
                };
            }
            else
            {
                result[traceIndex] = trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        MatchMethod.TitleMatch, 0f, false,
                        Detail: "no matching episode found")
                };
            }
        }

        return result;
    }

    private async Task<ItemTrace[]> EnrichMovies(
        ItemTrace[] itemTraces, TestScoreWithEnrichment msg, IActorRef enrichmentManager)
    {
        var matchedIndices = new List<(int TraceIndex, MovieCandidate Candidate)>();

        for (var i = 0; i < itemTraces.Length; i++)
        {
            var trace = itemTraces[i];
            if (!trace.Matched)
                continue;

            var airedAt = trace.Candidate.Timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(trace.Candidate.Timestamp)
                : (DateTimeOffset?)null;

            matchedIndices.Add((i, new MovieCandidate(
                matchedIndices.Count,
                trace.Candidate.Title,
                airedAt,
                trace.Candidate.Duration)));
        }

        if (matchedIndices.Count == 0)
            return itemTraces;

        var enrichRequest = new EnrichMovies(
            msg.ImdbId, msg.TmdbId,
            [.. matchedIndices.Select(m => m.Candidate)], msg.EnrichmentConfig!);

        var response = await enrichmentManager.Ask<EnrichMoviesResponse>(enrichRequest, _enrichmentTimeout);
        if (response is not EnrichMoviesCompleted completed)
            return itemTraces;

        var enrichedLookup = completed.Movies.ToDictionary(m => m.Index);
        var result = new ItemTrace[itemTraces.Length];
        Array.Copy(itemTraces, result, itemTraces.Length);

        foreach (var (traceIndex, candidate) in matchedIndices)
        {
            var trace = result[traceIndex];
            if (enrichedLookup.TryGetValue(candidate.Index, out var enriched))
            {
                result[traceIndex] = trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        enriched.Method, enriched.Confidence, true,
                        ResolvedTitle: enriched.Title, ResolvedYear: enriched.Year)
                };
            }
            else
            {
                result[traceIndex] = trace with
                {
                    EnrichmentTrace = new EnrichmentTrace(
                        MatchMethod.TitleMatch, 0f, false,
                        Detail: "no matching movie found")
                };
            }
        }

        return result;
    }
}
