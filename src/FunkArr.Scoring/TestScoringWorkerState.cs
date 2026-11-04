using Akka.Actor;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Scoring;

public sealed class TestScoringWorkerState
{
    private IActorRef _replyTo = ActorRefs.Nobody;
    private TestScoreWithEnrichment _request = null!;

    public static TestScoringWorkerState Empty => new();

    public void Apply(TestScoreWithEnrichment msg, IActorRef sender)
    {
        _request = msg;
        _replyTo = sender;
    }

    public void Reply(TestScoreWithEnrichmentResponse response) =>
        _replyTo.Tell(response);

    public bool HasEnrichmentConfig() => _request.EnrichmentConfig is not null;

    public bool IsShowWithTvdb() =>
        _request.MediaType == Messages.MediaType.Show && _request.TvdbId is not null;

    public bool IsMovieWithExternalId() =>
        _request.MediaType == Messages.MediaType.Movie &&
        (_request.TmdbId is not null || _request.ImdbId is not null);

    public EnrichEpisodes BuildEpisodeEnrichRequest(
        List<(int TraceIndex, EpisodeCandidate Candidate)> matchedIndices) =>
        new(_request.TvdbId!.Value, Season: null,
            [.. matchedIndices.Select(m => m.Candidate)], _request.EnrichmentConfig!);

    public EnrichMovies BuildMovieEnrichRequest(
        List<(int TraceIndex, MovieCandidate Candidate)> matchedIndices) =>
        new(_request.ImdbId, _request.TmdbId,
            [.. matchedIndices.Select(m => m.Candidate)], _request.EnrichmentConfig!);

    public List<(int TraceIndex, EpisodeCandidate Candidate)> BuildEpisodeCandidates(ItemTrace[] itemTraces)
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

    public List<(int TraceIndex, MovieCandidate Candidate)> BuildMovieCandidates(ItemTrace[] itemTraces)
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

    public ItemTrace[] MergeEpisodeEnrichment(
        EnrichEpisodesCompleted completed, ItemTrace[] itemTraces,
        List<(int TraceIndex, EpisodeCandidate Candidate)> matchedIndices)
    {
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

    public ItemTrace[] MergeMovieEnrichment(
        EnrichMoviesCompleted completed, ItemTrace[] itemTraces,
        List<(int TraceIndex, MovieCandidate Candidate)> matchedIndices)
    {
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
