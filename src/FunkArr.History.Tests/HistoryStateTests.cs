using FunkArr.Messages;
using FunkArr.Messages.History;
using FunkArr.Messages.Scoring;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.ScoringHistory;

namespace FunkArr.History.Tests;

public sealed class HistoryStateTests
{
    private static readonly DateTimeOffset _baseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

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

    [Fact]
    public void Apply_SingleEvent_AddsEntry()
    {
        var evt = CreateEvent(candidateCount: 10, matchedCount: 3, enrichedCount: 1);

        var state = HistoryState.Empty.Apply(evt);

        Assert.Single(state.Snapshots);
        Assert.Equal(evt.RequestId, state.Snapshots[0].RequestId);
        Assert.Equal(evt.CandidateCount, state.Snapshots[0].CandidateCount);
        Assert.Equal(evt.MatchedCount, state.Snapshots[0].MatchedCount);
        Assert.Equal(evt.EnrichedCount, state.Snapshots[0].EnrichedCount);
    }

    [Fact]
    public void Apply_MultipleEvents_AccumulatesEntries()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001"))
            .Apply(CreateEvent("a0000002"))
            .Apply(CreateEvent("a0000003"));

        Assert.Equal(3, state.Snapshots.Count);
    }

    [Fact]
    public void ProcessCommand_ReturnsNewStateAndEvent()
    {
        var requestId = Guid.NewGuid();
        var origin = new ScoringOrigin(SearchSource.Sonarr, "test-query");
        var cmd = new RecordHistory(requestId, "rs-1", origin, _baseTime, 10, 5, 3, []);

        var (newState, evt) = HistoryState.Empty.ProcessCommand(cmd);

        Assert.Single(newState.Snapshots);
        Assert.Equal(requestId, newState.Snapshots[0].RequestId);
        Assert.Equal(requestId, evt.RequestId);
        Assert.Equal(PersistedSearchSource.Sonarr, evt.Source);
        Assert.Equal("test-query", evt.Query);
        Assert.Equal(10, evt.CandidateCount);
        Assert.Equal(5, evt.MatchedCount);
        Assert.Equal(3, evt.EnrichedCount);
    }

    [Fact]
    public void ComputeStats_EmptyState_ReturnsNullRates()
    {
        var stats = HistoryState.Empty.Stats;

        Assert.Null(stats.LastRun);
        Assert.Null(stats.MatchRate);
        Assert.Null(stats.EnrichmentRate);
        Assert.Equal(0, stats.TotalRuns);
    }

    [Fact]
    public void ComputeStats_SingleEntry_CalculatesRates()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent(candidateCount: 10, matchedCount: 4, enrichedCount: 2));

        Assert.NotNull(state.Stats.LastRun);
        Assert.Equal(0.4, state.Stats.MatchRate!.Value, 0.001);
        Assert.Equal(0.5, state.Stats.EnrichmentRate!.Value, 0.001);
        Assert.Equal(1, state.Stats.TotalRuns);
    }

    [Fact]
    public void ComputeStats_ZeroCandidates_ExcludedFromMatchRate()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001", candidateCount: 0, matchedCount: 0, enrichedCount: 0))
            .Apply(CreateEvent("a0000002", candidateCount: 10, matchedCount: 5, enrichedCount: 3));

        Assert.Equal(0.5, state.Stats.MatchRate!.Value, 0.001);
        Assert.Equal(2, state.Stats.TotalRuns);
    }

    [Fact]
    public void ComputeStats_ZeroMatched_ExcludedFromEnrichmentRate()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001", candidateCount: 10, matchedCount: 0, enrichedCount: 0))
            .Apply(CreateEvent("a0000002", candidateCount: 10, matchedCount: 4, enrichedCount: 2));

        Assert.Equal(0.5, state.Stats.EnrichmentRate!.Value, 0.001);
    }

    [Fact]
    public void ComputeStats_AllZeroCandidates_MatchRateIsNull()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent(candidateCount: 0, matchedCount: 0, enrichedCount: 0));

        Assert.Null(state.Stats.MatchRate);
    }

    [Fact]
    public void ComputeStats_MultipleEntries_AveragesCorrectly()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001", candidateCount: 10, matchedCount: 2, enrichedCount: 1))
            .Apply(CreateEvent("a0000002", candidateCount: 10, matchedCount: 8, enrichedCount: 4));

        var expectedMatchRate = (2.0 / 10 + 8.0 / 10) / 2;
        var expectedEnrichmentRate = (1.0 / 2 + 4.0 / 8) / 2;

        Assert.Equal(expectedMatchRate, state.Stats.MatchRate!.Value, 0.001);
        Assert.Equal(expectedEnrichmentRate, state.Stats.EnrichmentRate!.Value, 0.001);
    }

    [Fact]
    public void Trim_UnderLimits_ReturnsSameState()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent(timestamp: DateTimeOffset.UtcNow));

        var trimmed = state.Trim(maxSnapshots: 100, maxAgeDays: 365);

        Assert.Same(state, trimmed);
    }

    [Fact]
    public void Trim_ExceedsMaxCount_KeepsNewest()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001", timestamp: DateTimeOffset.UtcNow.AddMinutes(-3)))
            .Apply(CreateEvent("a0000002", timestamp: DateTimeOffset.UtcNow.AddMinutes(-2)))
            .Apply(CreateEvent("a0000003", timestamp: DateTimeOffset.UtcNow.AddMinutes(-1)));

        var trimmed = state.Trim(maxSnapshots: 2, maxAgeDays: 365);

        Assert.Equal(2, trimmed.Snapshots.Count);
        Assert.Equal(ParseGuid("a0000002"), trimmed.Snapshots[0].RequestId);
        Assert.Equal(ParseGuid("a0000003"), trimmed.Snapshots[1].RequestId);
    }

    [Fact]
    public void Trim_OldEntries_RemovedByAge()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001", timestamp: DateTimeOffset.UtcNow.AddDays(-10)))
            .Apply(CreateEvent("a0000002", timestamp: DateTimeOffset.UtcNow));

        var trimmed = state.Trim(maxSnapshots: 100, maxAgeDays: 5);

        Assert.Single(trimmed.Snapshots);
        Assert.Equal(ParseGuid("a0000002"), trimmed.Snapshots[0].RequestId);
    }

    [Fact]
    public void Trim_EmptyState_ReturnsEmpty()
    {
        var trimmed = HistoryState.Empty.Trim(maxSnapshots: 10, maxAgeDays: 30);

        Assert.Same(HistoryState.Empty, trimmed);
    }

    [Fact]
    public void Trim_AllExpired_ReturnsEmptyState()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001", timestamp: DateTimeOffset.UtcNow.AddDays(-100)))
            .Apply(CreateEvent("a0000002", timestamp: DateTimeOffset.UtcNow.AddDays(-50)));

        var trimmed = state.Trim(maxSnapshots: 100, maxAgeDays: 30);

        Assert.Empty(trimmed.Snapshots);
    }

    [Fact]
    public void QueryHistory_ReturnsNewestFirst()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001", timestamp: _baseTime))
            .Apply(CreateEvent("a0000002", timestamp: _baseTime.AddHours(1)))
            .Apply(CreateEvent("a0000003", timestamp: _baseTime.AddHours(2)));

        var result = state.QueryHistory(new QueryScoringHistory("rs-1", 0, 10));

        Assert.Equal(3, result.Snapshots.Length);
        Assert.Equal(ParseGuid("a0000003"), result.Snapshots[0].RequestId);
        Assert.Equal(ParseGuid("a0000002"), result.Snapshots[1].RequestId);
        Assert.Equal(ParseGuid("a0000001"), result.Snapshots[2].RequestId);
    }

    [Fact]
    public void QueryHistory_RespectsOffsetAndLimit()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001", timestamp: _baseTime))
            .Apply(CreateEvent("a0000002", timestamp: _baseTime.AddHours(1)))
            .Apply(CreateEvent("a0000003", timestamp: _baseTime.AddHours(2)))
            .Apply(CreateEvent("a0000004", timestamp: _baseTime.AddHours(3)));

        var result = state.QueryHistory(new QueryScoringHistory("rs-1", 1, 2));

        Assert.Equal(2, result.Snapshots.Length);
        Assert.Equal(ParseGuid("a0000003"), result.Snapshots[0].RequestId);
        Assert.Equal(ParseGuid("a0000002"), result.Snapshots[1].RequestId);
    }

    [Fact]
    public void QueryHistory_OffsetBeyondCount_ReturnsEmpty()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent());

        var result = state.QueryHistory(new QueryScoringHistory("rs-1", 100, 10));

        Assert.Empty(result.Snapshots);
    }

    [Fact]
    public void QueryHistory_ReturnsTotalCount()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001"))
            .Apply(CreateEvent("a0000002"))
            .Apply(CreateEvent("a0000003"));

        var result = state.QueryHistory(new QueryScoringHistory("rs-1", 0, 1));

        Assert.Single(result.Snapshots);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public void QueryDetail_ExistingRequestId_ReturnsResult()
    {
        var requestId = ParseGuid("a0000001");
        var state = HistoryState.Empty
            .Apply(CreateEvent("a0000001", candidateCount: 10, matchedCount: 5, enrichedCount: 3));

        var response = state.QueryDetail(new QueryScoringDetail("rs-1", requestId));

        var result = Assert.IsType<ScoringDetailResult>(response);
        Assert.Equal(requestId, result.RequestId);
        Assert.Equal(SearchSource.Sonarr, result.Source);
        Assert.Equal("test-query", result.Query);
    }

    [Fact]
    public void QueryDetail_UnknownRequestId_ReturnsFailed()
    {
        var response = HistoryState.Empty.QueryDetail(
            new QueryScoringDetail("rs-1", Guid.NewGuid()));

        Assert.IsType<ScoringDetailFailed>(response);
    }

    [Fact]
    public void QueryHistory_MapsScoringSnapshotSummaryFields()
    {
        var state = HistoryState.Empty
            .Apply(CreateEvent(candidateCount: 10, matchedCount: 5));

        var result = state.QueryHistory(new QueryScoringHistory("rs-1", 0, 10));

        var summary = Assert.Single(result.Snapshots);
        Assert.Equal(SearchSource.Sonarr, summary.Source);
        Assert.Equal("test-query", summary.Query);
        Assert.Equal(10, summary.CandidateCount);
        Assert.Equal(5, summary.MatchedCount);
    }

    private static Guid ParseGuid(string shortId) =>
        Guid.Parse($"00000000-0000-0000-0000-{shortId.PadLeft(12, '0')}");

    private static HistoryRecorded CreateEvent(
        string requestId = "a0000001",
        int candidateCount = 5,
        int matchedCount = 2,
        int enrichedCount = 1,
        DateTimeOffset? timestamp = null) =>
        new(
            ParseGuid(requestId),
            PersistedSearchSource.Sonarr,
            "test-query",
            timestamp ?? _baseTime,
            candidateCount,
            matchedCount,
            enrichedCount,
            []);
}
