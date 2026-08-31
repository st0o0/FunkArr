using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Messages.Scoring;
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

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720) };
        manager.Tell(new ScoreItems(candidates, "test"));

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
            new ScoreCandidate("Test Title", "Test", "ARD", 5400, 720),
            new ScoreCandidate("Another Title", "Test", "ZDF", 3600, 1080),
        };

        manager.Tell(new ScoreItems(candidates, "nonexistent"));
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

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720) };
        manager.Tell(new ScoreItems(candidates, "test"));

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

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720) };

        manager.Tell(new ScoreItems(candidates, "show-a"));
        var resultA = ExpectMsg<ScoreCompleted>();
        Assert.Equal(0.8, resultA.Results[0].Score, 0.001);

        manager.Tell(new ScoreItems(candidates, "show-b"));
        var resultB = ExpectMsg<ScoreCompleted>();
        Assert.Equal(0.6, resultB.Results[0].Score, 0.001);
    }
}
