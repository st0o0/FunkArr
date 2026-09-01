using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Xunit;

namespace FunkArr.MatchMagic.Tests;

public sealed class MatchMagicManagerTests : TestKit
{
    private static MatchingConfig CreateAirdateConfig(string ruleSetId = "test", float confidence = 0.9f) =>
        new(ruleSetId, confidence, [
            new MatchingRule("airdate-rule", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.AirdateExtraction)),
        ]);

    [Fact]
    public void Scores_items_with_loaded_config()
    {
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager(1)));

        manager.Tell(CreateAirdateConfig());

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0) };
        manager.Tell(new ScoreItems(Guid.Empty, "test", new ScoringOrigin("test", "test"), candidates));

        var result = ExpectMsg<ScoreCompleted>();
        Assert.Single(result.Results);
        Assert.True(result.Results[0].Matched);
        Assert.Equal(0.9, result.Results[0].Score, 0.001);
    }

    [Fact]
    public void Returns_zeros_for_unknown_ruleset_id()
    {
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager(1)));

        var candidates = new[]
        {
            new ScoreCandidate("Test Title", "Test", "ARD", 5400, 720, null, 0),
            new ScoreCandidate("Another Title", "Test", "ZDF", 3600, 1080, null, 0),
        };

        manager.Tell(new ScoreItems(Guid.Empty, "nonexistent", new ScoringOrigin("test", "test"), candidates));
        var result = ExpectMsg<ScoreCompleted>();

        Assert.Equal(2, result.Results.Length);
        Assert.All(result.Results, r =>
        {
            Assert.Equal(0.0, r.Score);
            Assert.False(r.Matched);
        });
    }

    [Fact]
    public void Config_update_replaces_previous()
    {
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager(1)));

        manager.Tell(CreateAirdateConfig("test", 0.5f));
        manager.Tell(CreateAirdateConfig("test", 0.99f));

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0) };
        manager.Tell(new ScoreItems(Guid.Empty, "test", new ScoringOrigin("test", "test"), candidates));

        var result = ExpectMsg<ScoreCompleted>();
        Assert.Single(result.Results);
        Assert.True(result.Results[0].Matched);
        Assert.Equal(0.99, result.Results[0].Score, 0.001);
    }

    [Fact]
    public void Multiple_configs_stored_independently()
    {
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager(1)));

        manager.Tell(CreateAirdateConfig("show-a", 0.8f));
        manager.Tell(CreateAirdateConfig("show-b", 0.6f));

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0) };

        manager.Tell(new ScoreItems(Guid.Empty, "show-a", new ScoringOrigin("test", "test"), candidates));
        var resultA = ExpectMsg<ScoreCompleted>();
        Assert.Equal(0.8, resultA.Results[0].Score, 0.001);

        manager.Tell(new ScoreItems(Guid.Empty, "show-b", new ScoringOrigin("test", "test"), candidates));
        var resultB = ExpectMsg<ScoreCompleted>();
        Assert.Equal(0.6, resultB.Results[0].Score, 0.001);
    }

    [Fact]
    public void HistoryRef_forwarded_to_pool_workers()
    {
        var historyProbe = CreateTestProbe();
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager(1, historyProbe)));

        manager.Tell(CreateAirdateConfig());

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720, null, 0) };
        manager.Tell(new ScoreItems(Guid.NewGuid(), "test", new ScoringOrigin("sonarr", "Test"), candidates));

        ExpectMsg<ScoreCompleted>();
        var history = historyProbe.ExpectMsg<RecordScoringResult>();
        Assert.Equal("test", history.RuleSetId);
        Assert.Equal("sonarr", history.Origin.Source);
        Assert.Equal(1, history.CandidateCount);
        Assert.Equal(1, history.MatchedCount);
        Assert.Single(history.ItemTraces);
    }

    [Fact]
    public void Unknown_ruleset_returns_request_id()
    {
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager(1)));
        var requestId = Guid.NewGuid();

        var candidates = new[] { new ScoreCandidate("Test", "Test", "ARD", 5400, 720, null, 0) };
        manager.Tell(new ScoreItems(requestId, "nonexistent", new ScoringOrigin("test", "test"), candidates));

        var result = ExpectMsg<ScoreCompleted>();
        Assert.Equal(requestId, result.RequestId);
    }
}
