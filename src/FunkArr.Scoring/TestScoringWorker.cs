using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Terp.Akka;

namespace FunkArr.Scoring;

public sealed class TestScoringWorker : ReceiveActor
{
    private static readonly TimeSpan _enrichmentTimeout = TimeSpan.FromSeconds(10);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _scoringManager = Context.GetActor<IScoringManager>();

    private IActorRef _replyTo = ActorRefs.Nobody;
    private TestScoreWithEnrichment _request = null!;

    public TestScoringWorker()
    {

        Receive<TestScoreWithEnrichment>(HandleRequest);
    }

    private void HandleRequest(TestScoreWithEnrichment msg)
    {
        _request = msg;
        _replyTo = Sender;

        _scoringManager.Ask<ScoreItemsResponse>(
            new TestScoreItems(msg.TestScoringId, msg.Config, msg.Candidates),
            _enrichmentTimeout)
            .PipeTo(Self, success: r => r, failure: ex => new TestScoreWithEnrichmentFailed(ex));

        Become(WaitingForScore);
    }

    private void WaitingForScore()
    {
        Receive<ScoreCompleted>(HandleScoreCompleted);
        Receive<TestScoreWithEnrichmentFailed>(HandleFailure);
    }

    private void HandleScoreCompleted(ScoreCompleted completed)
    {
        var itemTraces = completed.ItemTraces;

        if (_request.EnrichmentConfig is null)
        {
            Reply(new TestScoreWithEnrichmentCompleted(itemTraces));
            return;
        }

        var isShow = _request.MediaType == Messages.MediaType.Show;
        var isMovie = _request.MediaType == Messages.MediaType.Movie;

        if (isShow && _request.TvdbId is not null)
        {
            RequestEpisodeEnrichment(itemTraces);
            return;
        }

        if (isMovie && (_request.TmdbId is not null || _request.ImdbId is not null))
        {
            RequestMovieEnrichment(itemTraces);
            return;
        }

        Reply(new TestScoreWithEnrichmentCompleted(itemTraces));
    }

    private void RequestEpisodeEnrichment(ItemTrace[] itemTraces)
    {
        var matchedIndices = BuildEpisodeCandidates(itemTraces);
        if (matchedIndices.Count == 0)
        {
            Reply(new TestScoreWithEnrichmentCompleted(itemTraces));
            return;
        }

        var enrichmentManager = Context.GetActor<IEnrichmentManager>();
        var enrichRequest = new EnrichEpisodes(
            _request.TvdbId!.Value, Season: null,
            [.. matchedIndices.Select(m => m.Candidate)], _request.EnrichmentConfig!);

        enrichmentManager.Ask<EnrichEpisodesResponse>(enrichRequest, _enrichmentTimeout)
            .PipeTo(Self,
                success: r => new EpisodeEnrichmentResult(r, itemTraces, matchedIndices),
                failure: _ => new EnrichmentFailed(itemTraces));

        Become(WaitingForEnrichment);
    }

    private void RequestMovieEnrichment(ItemTrace[] itemTraces)
    {
        var matchedIndices = BuildMovieCandidates(itemTraces);
        if (matchedIndices.Count == 0)
        {
            Reply(new TestScoreWithEnrichmentCompleted(itemTraces));
            return;
        }

        var enrichmentManager = Context.GetActor<IEnrichmentManager>();
        var enrichRequest = new EnrichMovies(
            _request.ImdbId, _request.TmdbId,
            [.. matchedIndices.Select(m => m.Candidate)], _request.EnrichmentConfig!);

        enrichmentManager.Ask<EnrichMoviesResponse>(enrichRequest, _enrichmentTimeout)
            .PipeTo(Self,
                success: r => new MovieEnrichmentResult(r, itemTraces, matchedIndices),
                failure: _ => new EnrichmentFailed(itemTraces));

        Become(WaitingForEnrichment);
    }

    private void WaitingForEnrichment()
    {
        Receive<EpisodeEnrichmentResult>(HandleEpisodeEnrichment);
        Receive<MovieEnrichmentResult>(HandleMovieEnrichment);
        Receive<EnrichmentFailed>(msg => Reply(new TestScoreWithEnrichmentCompleted(msg.ItemTraces)));
    }

    private void HandleEpisodeEnrichment(EpisodeEnrichmentResult msg)
    {
        if (msg.Response is not EnrichEpisodesCompleted completed)
        {
            Reply(new TestScoreWithEnrichmentCompleted(msg.ItemTraces));
            return;
        }

        var enrichedLookup = completed.Episodes.ToDictionary(e => e.Index);
        var result = new ItemTrace[msg.ItemTraces.Length];
        Array.Copy(msg.ItemTraces, result, msg.ItemTraces.Length);

        foreach (var (traceIndex, candidate) in msg.MatchedIndices)
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

        Reply(new TestScoreWithEnrichmentCompleted(result));
    }

    private void HandleMovieEnrichment(MovieEnrichmentResult msg)
    {
        if (msg.Response is not EnrichMoviesCompleted completed)
        {
            Reply(new TestScoreWithEnrichmentCompleted(msg.ItemTraces));
            return;
        }

        var enrichedLookup = completed.Movies.ToDictionary(m => m.Index);
        var result = new ItemTrace[msg.ItemTraces.Length];
        Array.Copy(msg.ItemTraces, result, msg.ItemTraces.Length);

        foreach (var (traceIndex, candidate) in msg.MatchedIndices)
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

        Reply(new TestScoreWithEnrichmentCompleted(result));
    }

    private void Reply(TestScoreWithEnrichmentResponse response)
    {
        _replyTo.Tell(response);
        Context.Parent.Tell(new Passivate(PoisonPill.Instance));
    }

    private void HandleFailure(TestScoreWithEnrichmentFailed failed)
    {
        _log.Warning(failed.Cause, "Test scoring failed");
        Reply(failed);
    }

    private static List<(int TraceIndex, EpisodeCandidate Candidate)> BuildEpisodeCandidates(ItemTrace[] itemTraces)
    {
        var matchedIndices = new List<(int TraceIndex, EpisodeCandidate Candidate)>();

        for (var i = 0; i < itemTraces.Length; i++)
        {
            var trace = itemTraces[i];
            if (!trace.Matched)
            {
                continue;
            }

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

        return matchedIndices;
    }

    private static List<(int TraceIndex, MovieCandidate Candidate)> BuildMovieCandidates(ItemTrace[] itemTraces)
    {
        var matchedIndices = new List<(int TraceIndex, MovieCandidate Candidate)>();

        for (var i = 0; i < itemTraces.Length; i++)
        {
            var trace = itemTraces[i];
            if (!trace.Matched)
            {
                continue;
            }

            var airedAt = trace.Candidate.Timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(trace.Candidate.Timestamp)
                : (DateTimeOffset?)null;

            matchedIndices.Add((i, new MovieCandidate(
                matchedIndices.Count,
                trace.Candidate.Title,
                airedAt,
                trace.Candidate.Duration)));
        }

        return matchedIndices;
    }

    private sealed record EpisodeEnrichmentResult(
        EnrichEpisodesResponse Response, ItemTrace[] ItemTraces,
        List<(int TraceIndex, EpisodeCandidate Candidate)> MatchedIndices);

    private sealed record MovieEnrichmentResult(
        EnrichMoviesResponse Response, ItemTrace[] ItemTraces,
        List<(int TraceIndex, MovieCandidate Candidate)> MatchedIndices);

    private sealed record EnrichmentFailed(ItemTrace[] ItemTraces);
}
