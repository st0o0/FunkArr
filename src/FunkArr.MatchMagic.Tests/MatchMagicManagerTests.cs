using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Messages.Scoring;
using Xunit;

namespace FunkArr.MatchMagic.Tests;

public sealed class MatchMagicManagerTests : TestKit
{
    private static string CreateSimpleRuleSetJson() =>
        new RuleSet(
            "Test",
            Media: new MediaRef(Name: "Test"),
            Confidence: 0.9f,
            Rules: [new Rule("airdate-rule", 0, 0.9f, MatchStrategy.ItemTitleEqualsAirdate, new FilterGroup())])
        .ToJson();

    [Fact]
    public void Scores_items_with_loaded_ruleset()
    {
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager()));

        manager.Tell(new LoadRuleSet("test", CreateSimpleRuleSetJson()));

        var candidates = new[]
        {
            new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720),
        };

        manager.Tell(new ScoreItems(candidates, "test"));
        var result = ExpectMsg<ScoreCompleted>();

        Assert.Single(result.Results);
        Assert.Equal(0, result.Results[0].Index);
        Assert.True(result.Results[0].Matched);
        Assert.True(result.Results[0].Score > 0);
    }

    [Fact]
    public void Returns_default_scores_when_no_ruleset_loaded()
    {
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager()));

        var candidates = new[]
        {
            new ScoreCandidate("Test Title", "Test", "ARD", 5400, 720),
            new ScoreCandidate("Another Title", "Test", "ZDF", 3600, 1080),
        };

        manager.Tell(new ScoreItems(candidates, null));
        var result = ExpectMsg<ScoreCompleted>();

        Assert.Equal(2, result.Results.Length);
        Assert.All(result.Results, r =>
        {
            Assert.Equal(0.0, r.Score);
            Assert.False(r.Matched);
        });
    }

    [Fact]
    public void Unload_removes_ruleset()
    {
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager()));

        manager.Tell(new LoadRuleSet("test", CreateSimpleRuleSetJson()));
        manager.Tell(new UnloadRuleSet("test"));

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720) };
        manager.Tell(new ScoreItems(candidates, "test"));
        var result = ExpectMsg<ScoreCompleted>();

        Assert.Single(result.Results);
        Assert.False(result.Results[0].Matched);
    }

    [Fact]
    public void Falls_back_to_first_loaded_when_id_not_found()
    {
        var manager = Sys.ActorOf(Props.Create(() => new MatchMagicManager()));

        manager.Tell(new LoadRuleSet("default", CreateSimpleRuleSetJson()));

        var candidates = new[] { new ScoreCandidate("Sendung vom 24.10.2024", "Test", "ARD", 5400, 720) };
        manager.Tell(new ScoreItems(candidates, "nonexistent"));
        var result = ExpectMsg<ScoreCompleted>();

        Assert.Single(result.Results);
        Assert.True(result.Results[0].Matched);
    }
}
