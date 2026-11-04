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
    private readonly IActorRef _enrichmentManager = Context.GetActor<IEnrichmentManager>();
    private readonly TestScoringWorkerState _state = TestScoringWorkerState.Empty;

    public TestScoringWorker()
    {
        Receive<TestScoreWithEnrichment>(HandleRequest);
    }

    private void HandleRequest(TestScoreWithEnrichment msg)
    {
        _state.Apply(msg, Sender);

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

        if (!_state.HasEnrichmentConfig())
        {
            Reply(new TestScoreWithEnrichmentCompleted(itemTraces));
            return;
        }

        if (_state.IsShowWithTvdb())
        {
            RequestEpisodeEnrichment(itemTraces);
            return;
        }

        if (_state.IsMovieWithExternalId())
        {
            RequestMovieEnrichment(itemTraces);
            return;
        }

        Reply(new TestScoreWithEnrichmentCompleted(itemTraces));
    }

    private void RequestEpisodeEnrichment(ItemTrace[] itemTraces)
    {
        var matchedIndices = _state.BuildEpisodeCandidates(itemTraces);
        if (matchedIndices.Count == 0)
        {
            Reply(new TestScoreWithEnrichmentCompleted(itemTraces));
            return;
        }

        var enrichRequest = _state.BuildEpisodeEnrichRequest(matchedIndices);

        _enrichmentManager.Ask<EnrichEpisodesResponse>(enrichRequest, _enrichmentTimeout)
            .PipeTo(Self,
                success: r => new EpisodeEnrichmentResult(r, itemTraces, matchedIndices),
                failure: _ => new EnrichmentFailed(itemTraces));

        Become(WaitingForEnrichment);
    }

    private void RequestMovieEnrichment(ItemTrace[] itemTraces)
    {
        var matchedIndices = _state.BuildMovieCandidates(itemTraces);
        if (matchedIndices.Count == 0)
        {
            Reply(new TestScoreWithEnrichmentCompleted(itemTraces));
            return;
        }

        var enrichRequest = _state.BuildMovieEnrichRequest(matchedIndices);

        _enrichmentManager.Ask<EnrichMoviesResponse>(enrichRequest, _enrichmentTimeout)
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

        var result = _state.MergeEpisodeEnrichment(completed, msg.ItemTraces, msg.MatchedIndices);
        Reply(new TestScoreWithEnrichmentCompleted(result));
    }

    private void HandleMovieEnrichment(MovieEnrichmentResult msg)
    {
        if (msg.Response is not EnrichMoviesCompleted completed)
        {
            Reply(new TestScoreWithEnrichmentCompleted(msg.ItemTraces));
            return;
        }

        var result = _state.MergeMovieEnrichment(completed, msg.ItemTraces, msg.MatchedIndices);
        Reply(new TestScoreWithEnrichmentCompleted(result));
    }

    private void Reply(TestScoreWithEnrichmentResponse response)
    {
        _state.Reply(response);
        Context.Parent.Tell(new Passivate(PoisonPill.Instance));
    }

    private void HandleFailure(TestScoreWithEnrichmentFailed failed)
    {
        _log.Warning(failed.Cause, "Test scoring failed");
        Reply(failed);
    }

    private sealed record EpisodeEnrichmentResult(
        EnrichEpisodesResponse Response, ItemTrace[] ItemTraces,
        List<(int TraceIndex, EpisodeCandidate Candidate)> MatchedIndices);

    private sealed record MovieEnrichmentResult(
        EnrichMoviesResponse Response, ItemTrace[] ItemTraces,
        List<(int TraceIndex, MovieCandidate Candidate)> MatchedIndices);

    private sealed record EnrichmentFailed(ItemTrace[] ItemTraces);
}
