using FunkArr.Messages;
using FunkArr.Messages.History;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.ScoringHistory;

namespace FunkArr.History.Tests;

public sealed class HistoryStateTests
{
    [Fact]
    public void PersistenceRoundTrip_PreservesSnapshots()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent())
            .Apply(CreateEvent("a0000002"));

        var persisted = state.GetPersistenceState();
        var restored = HistoryState.FromPersistence(persisted);

        Assert.Equal(state.Snapshots.Count, restored.Snapshots.Count);
        Assert.Equal(state.Snapshots[0].RequestId, restored.Snapshots[0].RequestId);
        Assert.Equal(state.Snapshots[1].RequestId, restored.Snapshots[1].RequestId);
        Assert.Equal(state.Stats.TotalRuns, restored.Stats.TotalRuns);
    }

    [Fact]
    public void SnapshotRoundTrip_PreservesSnapshots()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent());

        var snapshot = state.GetSnapshot();
        var restored = HistoryState.FromPersistence(snapshot);

        Assert.Equal(state.Snapshots.Count, restored.Snapshots.Count);
        Assert.Equal(state.Snapshots[0].RequestId, restored.Snapshots[0].RequestId);
    }

    [Fact]
    public void PersistenceRoundTrip_EmptyState()
    {
        var persisted = HistoryState.Empty.GetPersistenceState();
        var restored = HistoryState.FromPersistence(persisted);

        Assert.Empty(restored.Snapshots);
        Assert.Null(restored.Stats.LastRun);
    }

    [Fact]
    public void PersistenceRoundTrip_PreservesStats()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent(candidateCount: 10, matchedCount: 5, enrichedCount: 3));

        var restored = HistoryState.FromPersistence(state.GetPersistenceState());

        Assert.Equal(state.Stats.LastRun, restored.Stats.LastRun);
        Assert.Equal(state.Stats.MatchRate, restored.Stats.MatchRate);
        Assert.Equal(state.Stats.EnrichmentRate, restored.Stats.EnrichmentRate);
        Assert.Equal(state.Stats.TotalRuns, restored.Stats.TotalRuns);
    }

    private static HistoryRecorded CreateEvent(
        string requestId = "a0000001",
        int candidateCount = 5,
        int matchedCount = 2,
        int enrichedCount = 1) =>
        new(
            Guid.Parse($"00000000-0000-0000-0000-{requestId.PadLeft(12, '0')}"),
            SearchSource.Sonarr,
            "test-query",
            DateTimeOffset.UtcNow,
            candidateCount,
            matchedCount,
            enrichedCount,
            []);
}
