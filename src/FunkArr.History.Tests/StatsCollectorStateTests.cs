using FunkArr.Messages.History;

namespace FunkArr.History.Tests;

public sealed class StatsCollectorStateTests
{
    private static ScoringStatsResult MakeStats(double matchRate = 0.5, int totalRuns = 10) =>
        new(DateTimeOffset.UtcNow, matchRate, 0.3, totalRuns);

    [Fact]
    public void Empty_HasNoEntries()
    {
        Assert.Empty(StatsCollectorState.Empty.Stats);
    }

    [Fact]
    public void Apply_UpdateStats_AddsEntry()
    {
        var stats = MakeStats();
        var state = StatsCollectorState.Empty
            .Apply(new UpdateStats("rs-1", stats));

        Assert.Single(state.Stats);
        Assert.Equal(stats, state.Stats["rs-1"]);
    }

    [Fact]
    public void Apply_UpdateStats_SameRuleSet_Replaces()
    {
        var first = MakeStats(matchRate: 0.3);
        var second = MakeStats(matchRate: 0.7);

        var state = StatsCollectorState.Empty
            .Apply(new UpdateStats("rs-1", first))
            .Apply(new UpdateStats("rs-1", second));

        Assert.Single(state.Stats);
        Assert.Equal(0.7, state.Stats["rs-1"].MatchRate);
    }

    [Fact]
    public void Apply_UpdateStats_MultipleRuleSets()
    {
        var state = StatsCollectorState.Empty
            .Apply(new UpdateStats("rs-1", MakeStats()))
            .Apply(new UpdateStats("rs-2", MakeStats()));

        Assert.Equal(2, state.Stats.Count);
    }

    [Fact]
    public void Apply_RemoveStats_RemovesEntry()
    {
        var state = StatsCollectorState.Empty
            .Apply(new UpdateStats("rs-1", MakeStats()))
            .Apply(new RemoveStats("rs-1"));

        Assert.Empty(state.Stats);
    }

    [Fact]
    public void Apply_RemoveStats_NonExistent_NoOp()
    {
        var state = StatsCollectorState.Empty
            .Apply(new RemoveStats("rs-nonexistent"));

        Assert.Empty(state.Stats);
    }

    [Fact]
    public void GetSnapshot_ReturnsAllEntries()
    {
        var state = StatsCollectorState.Empty
            .Apply(new UpdateStats("rs-1", MakeStats()))
            .Apply(new UpdateStats("rs-2", MakeStats()));

        var snapshot = state.GetSnapshot();

        Assert.Equal(2, snapshot.Entries.Count);
        Assert.True(snapshot.Entries.ContainsKey("rs-1"));
        Assert.True(snapshot.Entries.ContainsKey("rs-2"));
    }

    [Fact]
    public void FromSnapshot_RestoresState()
    {
        var stats = MakeStats(matchRate: 0.42, totalRuns: 7);
        var state = StatsCollectorState.Empty
            .Apply(new UpdateStats("rs-1", stats));

        var snapshot = state.GetSnapshot();
        var restored = StatsCollectorState.FromSnapshot(snapshot);

        Assert.Single(restored.Stats);
        Assert.Equal(0.42, restored.Stats["rs-1"].MatchRate);
        Assert.Equal(7, restored.Stats["rs-1"].TotalRuns);
    }

    [Fact]
    public void SnapshotRoundTrip_PreservesEntries()
    {
        var state = StatsCollectorState.Empty
            .Apply(new UpdateStats("rs-1", MakeStats(matchRate: 0.1)))
            .Apply(new UpdateStats("rs-2", MakeStats(matchRate: 0.9)));

        var restored = StatsCollectorState.FromSnapshot(state.GetSnapshot());
        var reSnapshot = restored.GetSnapshot();

        Assert.Equal(state.GetSnapshot().Entries.Count, reSnapshot.Entries.Count);
        Assert.Equal(
            state.GetSnapshot().Entries["rs-1"].MatchRate,
            reSnapshot.Entries["rs-1"].MatchRate);
        Assert.Equal(
            state.GetSnapshot().Entries["rs-2"].MatchRate,
            reSnapshot.Entries["rs-2"].MatchRate);
    }

    [Fact]
    public void SnapshotRoundTrip_EmptyState()
    {
        var snapshot = StatsCollectorState.Empty.GetSnapshot();
        var restored = StatsCollectorState.FromSnapshot(snapshot);

        Assert.Empty(restored.Stats);
    }
}
