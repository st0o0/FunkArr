using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;

namespace FunkArr.Scoring.Tests;

public sealed class TestScoringWorkerTests : TestKit
{
    private static readonly Guid _testScoringId = Guid.NewGuid();

    private static MatchingConfig TestConfig(float confidence = 0.9f) =>
        new("test", confidence, [
            new MatchingRule("rule-1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.AirdateExtraction))
        ]);

    private static ScoreCandidate[] TestCandidates() =>
    [
        new("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0)
    ];

    private static ItemTrace MatchedTrace() =>
        new(new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 1698105600),
            Matched: true, Score: 0.9, MatchedRuleId: "rule-1",
            Identification: new TracedIdentification("01", "05", "Episode Title"),
            RuleTraces: [new RuleTrace("rule-1", 0, RuleOutcome.Matched, null, null)]);

    private static ItemTrace UnmatchedTrace() =>
        new(new ScoreCandidate("Other", "Other", "ZDF", 3600, 1080, null, 0),
            Matched: false, Score: 0.0, MatchedRuleId: null,
            Identification: null,
            RuleTraces: [new RuleTrace("rule-1", 0, RuleOutcome.FilterFailed, null, null)]);

    private IActorRef CreateWorker(out TestProbe scoringProbe, out TestProbe enrichmentProbe)
    {
        scoringProbe = CreateTestProbe();
        enrichmentProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IScoringManager>(scoringProbe);
        registry.Register<IEnrichmentManager>(enrichmentProbe);

        return Sys.ActorOf(Props.Create(() => new TestScoringWorker()));
    }

    [Fact]
    public void Scores_without_enrichment()
    {
        var worker = CreateWorker(out var scoringProbe, out _);
        var config = TestConfig();
        var candidates = TestCandidates();

        worker.Tell(new TestScoreWithEnrichment(_testScoringId, config, candidates));

        var scoreMsg = scoringProbe.ExpectMsg<TestScoreItems>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(_testScoringId, scoreMsg.TestScoringId);

        var traces = new[] { MatchedTrace() };
        scoringProbe.Reply(new ScoreCompleted(_testScoringId, [], traces));

        var result = ExpectMsg<TestScoreWithEnrichmentCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var trace = Assert.Single(result.ItemTraces);
        Assert.True(trace.Matched);
        Assert.Equal(0.9, trace.Score);
    }

    [Fact]
    public void Scores_with_episode_enrichment()
    {
        var worker = CreateWorker(out var scoringProbe, out var enrichmentProbe);
        var config = TestConfig();
        var candidates = TestCandidates();
        var enrichmentConfig = new EnrichmentConfig(true, [EnrichmentMethod.Title],
            new TitleMatchConfig(0.5f), new AirdateMatchConfig(3), new RuntimeMatchConfig(0.1f, RuntimeMode.Tiebreaker),
            new YearMatchConfig(1));

        worker.Tell(new TestScoreWithEnrichment(
            _testScoringId, config, candidates,
            MediaType: Messages.MediaType.Show, TvdbId: 12345,
            EnrichmentConfig: enrichmentConfig));

        var scoreMsg = scoringProbe.ExpectMsg<TestScoreItems>(cancellationToken: TestContext.Current.CancellationToken);
        var traces = new[] { MatchedTrace() };
        scoringProbe.Reply(new ScoreCompleted(_testScoringId, [], traces));

        var enrichMsg = enrichmentProbe.ExpectMsg<EnrichEpisodes>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(12345, enrichMsg.TvdbId);

        enrichmentProbe.Reply(new EnrichEpisodesCompleted([
            new EnrichedEpisode(0, "1", "5", "Episode Title", 0.95f, MatchMethod.TitleMatch)
        ]));

        var result = ExpectMsg<TestScoreWithEnrichmentCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var trace = Assert.Single(result.ItemTraces);
        Assert.NotNull(trace.EnrichmentTrace);
        Assert.True(trace.EnrichmentTrace.Enriched);
        Assert.Equal(MatchMethod.TitleMatch, trace.EnrichmentTrace.Method);
    }

    [Fact]
    public void Scores_with_movie_enrichment()
    {
        var worker = CreateWorker(out var scoringProbe, out var enrichmentProbe);
        var config = TestConfig();
        var candidates = TestCandidates();
        var enrichmentConfig = new EnrichmentConfig(true, [EnrichmentMethod.Title],
            new TitleMatchConfig(0.5f), new AirdateMatchConfig(3), new RuntimeMatchConfig(0.1f, RuntimeMode.Tiebreaker),
            new YearMatchConfig(1));

        worker.Tell(new TestScoreWithEnrichment(
            _testScoringId, config, candidates,
            MediaType: Messages.MediaType.Movie, TmdbId: 550,
            EnrichmentConfig: enrichmentConfig));

        var scoreMsg = scoringProbe.ExpectMsg<TestScoreItems>(cancellationToken: TestContext.Current.CancellationToken);
        var traces = new[] { MatchedTrace() };
        scoringProbe.Reply(new ScoreCompleted(_testScoringId, [], traces));

        var enrichMsg = enrichmentProbe.ExpectMsg<EnrichMovies>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(550, enrichMsg.TmdbId);

        enrichmentProbe.Reply(new EnrichMoviesCompleted([
            new EnrichedMovie(0, "Fight Club", 1999, null, 550, 0.98f, MatchMethod.TitleMatch)
        ]));

        var result = ExpectMsg<TestScoreWithEnrichmentCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var trace = Assert.Single(result.ItemTraces);
        Assert.NotNull(trace.EnrichmentTrace);
        Assert.True(trace.EnrichmentTrace.Enriched);
        Assert.Equal(MatchMethod.TitleMatch, trace.EnrichmentTrace.Method);
    }

    [Fact]
    public void Enrichment_failure_returns_scoring_only_results()
    {
        var worker = CreateWorker(out var scoringProbe, out var enrichmentProbe);
        var config = TestConfig();
        var candidates = TestCandidates();
        var enrichmentConfig = new EnrichmentConfig(true, [EnrichmentMethod.Title],
            new TitleMatchConfig(0.5f), new AirdateMatchConfig(3), new RuntimeMatchConfig(0.1f, RuntimeMode.Tiebreaker),
            new YearMatchConfig(1));

        worker.Tell(new TestScoreWithEnrichment(
            _testScoringId, config, candidates,
            MediaType: Messages.MediaType.Show, TvdbId: 12345,
            EnrichmentConfig: enrichmentConfig));

        scoringProbe.ExpectMsg<TestScoreItems>(cancellationToken: TestContext.Current.CancellationToken);
        var traces = new[] { MatchedTrace() };
        scoringProbe.Reply(new ScoreCompleted(_testScoringId, [], traces));

        enrichmentProbe.ExpectMsg<EnrichEpisodes>(cancellationToken: TestContext.Current.CancellationToken);
        enrichmentProbe.Reply(new EnrichEpisodesFailed(new TimeoutException("enrichment timed out")));

        var result = ExpectMsg<TestScoreWithEnrichmentCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        var trace = Assert.Single(result.ItemTraces);
        Assert.True(trace.Matched);
        Assert.Null(trace.EnrichmentTrace);
    }

    [Fact]
    public void Scoring_failure_returns_failed_response()
    {
        var worker = CreateWorker(out var scoringProbe, out _);
        var config = TestConfig();
        var candidates = TestCandidates();

        worker.Tell(new TestScoreWithEnrichment(_testScoringId, config, candidates));

        scoringProbe.ExpectMsg<TestScoreItems>(cancellationToken: TestContext.Current.CancellationToken);
        scoringProbe.Reply("unexpected-response-type");

        var result = ExpectMsg<TestScoreWithEnrichmentFailed>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result.Cause);
    }
}
