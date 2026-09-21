using FunkArr.Messages.Scoring;

namespace FunkArr.Scoring.Tests;

public sealed class ScoringManagerStateTests
{
    [Fact]
    public void Apply_AddsConfig()
    {
        var config = new MatchingConfig("tatort", 0.8f, [
            new MatchingRule("rule-1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.AirdateExtraction)),
        ]);

        var state = ScoringManagerState.Empty.Apply(config);

        Assert.Single(state.Configs);
        Assert.NotNull(state.GetConfig("tatort"));
        Assert.Equal(0.8f, state.GetConfig("tatort")!.DefaultConfidence);
    }

    [Fact]
    public void Apply_MultipleConfigs()
    {
        var config1 = new MatchingConfig("tatort", 0.8f, []);
        var config2 = new MatchingConfig("heute-show", 0.9f, []);

        var state = ScoringManagerState.Empty
            .Apply(config1)
            .Apply(config2);

        Assert.Equal(2, state.Configs.Count);
        Assert.NotNull(state.GetConfig("tatort"));
        Assert.NotNull(state.GetConfig("heute-show"));
    }

    [Fact]
    public void Apply_RemoveConfig()
    {
        var config = new MatchingConfig("tatort", 0.8f, []);

        var state = ScoringManagerState.Empty
            .Apply(config)
            .Apply(new RemoveMatchingConfig("tatort"));

        Assert.Empty(state.Configs);
    }

    [Fact]
    public void GetConfig_NonExistent_ReturnsNull()
    {
        var result = ScoringManagerState.Empty.GetConfig("nonexistent");

        Assert.Null(result);
    }
}
