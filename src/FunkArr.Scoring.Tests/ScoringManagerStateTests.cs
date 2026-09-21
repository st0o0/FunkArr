using FunkArr.Messages.Scoring;

namespace FunkArr.Scoring.Tests;

public sealed class ScoringManagerStateTests
{
    [Fact]
    public void SnapshotRoundTrip_PreservesConfigs()
    {
        var config = new MatchingConfig("tatort", 0.8f, [
            new MatchingRule("rule-1", 0, null, null,
                new IdentificationSpec(IdentificationStrategy.AirdateExtraction)),
        ]);

        var state = ScoringManagerState.Empty.Apply(config);

        var snapshot = state.GetSnapshot();
        var restored = ScoringManagerState.FromSnapshot(snapshot);

        Assert.Equal(state.Configs.Count, restored.Configs.Count);
        Assert.NotNull(restored.GetConfig("tatort"));
        Assert.Equal(0.8f, restored.GetConfig("tatort")!.DefaultConfidence);
    }

    [Fact]
    public void SnapshotRoundTrip_EmptyState()
    {
        var snapshot = ScoringManagerState.Empty.GetSnapshot();
        var restored = ScoringManagerState.FromSnapshot(snapshot);

        Assert.Empty(restored.Configs);
    }

    [Fact]
    public void SnapshotRoundTrip_MultipleConfigs()
    {
        var config1 = new MatchingConfig("tatort", 0.8f, []);
        var config2 = new MatchingConfig("heute-show", 0.9f, []);

        var state = ScoringManagerState.Empty
            .Apply(config1)
            .Apply(config2);

        var restored = ScoringManagerState.FromSnapshot(state.GetSnapshot());

        Assert.Equal(2, restored.Configs.Count);
        Assert.NotNull(restored.GetConfig("tatort"));
        Assert.NotNull(restored.GetConfig("heute-show"));
    }
}
