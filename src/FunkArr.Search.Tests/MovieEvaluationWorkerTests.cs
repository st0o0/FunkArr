using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using Microsoft.Extensions.Options;

namespace FunkArr.Search.Tests;

public sealed class MovieEvaluationWorkerTests : TestKit
{
    private sealed record TestProbes(
        Akka.TestKit.TestProbe Scoring,
        Akka.TestKit.TestProbe Resolver,
        Akka.TestKit.TestProbe Enrichment,
        Akka.TestKit.TestProbe History);

    private TestProbes RegisterProbes()
    {
        var probes = new TestProbes(CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe());
        var registry = ActorRegistry.For(Sys);
        registry.Register<IScoringManager>(probes.Scoring);
        registry.Register<IRuleSetResolver>(probes.Resolver);
        registry.Register<IEnrichmentManager>(probes.Enrichment);
        registry.Register<IHistoryRegion>(probes.History);
        return probes;
    }

    private IActorRef CreateWorker(Guid evaluationId) =>
        Sys.ActorOf(Props.Create(() => new MovieEvaluationWorker(evaluationId.ToString(), (IOptionsMonitor<VariantOptions>?)null)));

    [Fact]
    public void Evaluation_resolves_ruleset_from_topic_and_returns_scored_results()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateMovies(evaluationId, SearchSource.Radarr,
        [
            new MediathekItem("ARD", "Das Boot", "Das Boot", null, 1719244800, 7200, 2400000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null)
        ]), TestActor);

        var resolveRequest = p.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Equal("Das Boot", resolveRequest.TopicOrAlias);
        p.Resolver.Reply(new RuleSetResolved("das-boot", "Das Boot"));

        var scoreRequest = p.Scoring.ExpectMsg<ScoreItems>();
        Assert.Equal("das-boot", scoreRequest.RuleSetId);
        p.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.9, true)], []));

        var result = ExpectMsg<EvaluateMoviesCompleted>();
        Assert.Equal(evaluationId, result.SearchId);
        Assert.Equal(2, result.Items.Length);
        Assert.Contains(result.Items, i => i.Quality == 1080);
        Assert.Contains(result.Items, i => i.Quality == 720);
        p.Enrichment.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void RuleSet_not_found_returns_unscored_results()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateMovies(evaluationId, SearchSource.Radarr, [Item("Unknown Movie")]), TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetFailed(new RuleSetNotFoundException("Unknown Movie")));

        var result = ExpectMsg<EvaluateMoviesCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Equal(0.0, item.Score);
    }

    [Fact]
    public void Imdb_id_enrichment_failure_keeps_ids_on_results()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateMovies(evaluationId, SearchSource.Radarr,
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Der Film", null, 1719244800, 7200, 2400000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null)
        ], ImdbId: "tt0806910", RuleSet: new RuleSetResolved("tatort", "Tatort")), TestActor);

        var scoreRequest = p.Scoring.ExpectMsg<ScoreItems>();
        Assert.Equal("tatort", scoreRequest.RuleSetId);
        p.Resolver.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
        p.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.85, true)], []));

        var enrichRequest = p.Enrichment.ExpectMsg<EnrichMovies>();
        Assert.Equal("tt0806910", enrichRequest.ImdbId);
        p.Enrichment.Reply(new EnrichMoviesFailed(new Exception("Not configured")));

        var result = ExpectMsg<EvaluateMoviesCompleted>();
        Assert.Equal(2, result.Items.Length);
        Assert.All(result.Items, i => Assert.Multiple(
            () => Assert.Equal("tt0806910", i.Metadata?.Ids?.ImdbId),
            () => Assert.Null(i.Metadata?.MatchConfidence)));
    }

    [Fact]
    public void Feed_evaluation_with_tmdb_id_enriches_matched_items()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateMovies(evaluationId, SearchSource.Feed, [Item("Fight Club")], TmdbId: 550), TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetResolved("fight-club", "Fight Club"));

        var score = p.Scoring.ExpectMsg<ScoreItems>();
        Assert.Equal(SearchSource.Feed, score.Origin.Source);
        p.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.7, true)], []));

        var enrichRequest = p.Enrichment.ExpectMsg<EnrichMovies>();
        Assert.Equal(550, enrichRequest.TmdbId);
        p.Enrichment.Reply(new EnrichMoviesCompleted(
        [
            new EnrichedMovie(0, "Fight Club", 1999, null, 550, 0.95f, MatchMethod.TitleMatch)
        ]));

        var result = ExpectMsg<EvaluateMoviesCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Multiple(
            () => Assert.Equal(550, item.Metadata?.Ids?.TmdbId),
            () => Assert.Equal(0.95f, item.Metadata?.MatchConfidence),
            () => Assert.Equal(MatchMethod.TitleMatch, item.Metadata?.MatchMethod));
    }

    [Fact]
    public void Scoring_failure_returns_evaluation_failed()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateMovies(evaluationId, SearchSource.Radarr, [Item("Das Boot")],
            RuleSet: new RuleSetResolved("das-boot", "Das Boot")), TestActor);

        p.Scoring.ExpectMsg<ScoreItems>();
        p.Scoring.Reply(new ScoringFailed(new Exception("Scoring error")));

        var result = ExpectMsg<EvaluateMoviesFailed>();
        Assert.Multiple(
            () => Assert.Equal(evaluationId, result.SearchId),
            () => Assert.Contains("Scoring error", result.Cause.Message));
    }

    private static MediathekItem Item(string topic) =>
        new("ARD", topic, topic, null, 1719244800, 7200, 0, null, "https://x.com/v.mp4", null, null, null);
}
