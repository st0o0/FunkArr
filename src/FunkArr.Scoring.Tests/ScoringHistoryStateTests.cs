using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.ScoringHistory;

namespace FunkArr.Scoring.Tests;

public sealed class ScoringHistoryStateTests
{
    private static ScoringRecorded CreateEvent(
        Guid? requestId = null,
        DateTimeOffset? timestamp = null) => new(
        RequestId: requestId ?? Guid.NewGuid(),
        Source: "test",
        Query: "TestQuery",
        Timestamp: timestamp ?? DateTimeOffset.UtcNow,
        CandidateCount: 1,
        MatchedCount: 1,
        ItemTraces: [new ItemTrace("Title", "Topic", "ARD", 3600, 720, null, 0, true, 0.9, "r1", null, [])]);

    private static RecordScoring CreateCommand(string ruleSetId = "test") => new(
        RequestId: Guid.NewGuid(),
        RuleSetId: ruleSetId,
        Origin: new ScoringOrigin("sonarr", "Query"),
        Timestamp: DateTimeOffset.UtcNow,
        CandidateCount: 2,
        MatchedCount: 1,
        ItemTraces: [new ItemTrace("Title", "Topic", "ARD", 3600, 720, null, 0, true, 0.9, "r1", null, [])]);

    [Fact]
    public void Empty_state_has_no_snapshots()
    {
        Assert.Empty(ScoringHistoryState.Empty.Snapshots);
    }

    [Fact]
    public void Apply_adds_snapshot()
    {
        var state = ScoringHistoryState.Empty.Apply(CreateEvent());
        Assert.Single(state.Snapshots);
    }

    [Fact]
    public void Apply_preserves_event_data()
    {
        var evt = CreateEvent(requestId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var state = ScoringHistoryState.Empty.Apply(evt);

        var snapshot = state.Snapshots[0];
        Assert.Equal(evt.RequestId, snapshot.RequestId);
        Assert.Equal(evt.Source, snapshot.Origin.Source);
        Assert.Equal(evt.Query, snapshot.Origin.Query);
        Assert.Equal(evt.Timestamp, snapshot.Timestamp);
        Assert.Equal(evt.CandidateCount, snapshot.CandidateCount);
        Assert.Equal(evt.MatchedCount, snapshot.MatchedCount);
    }

    [Fact]
    public void Apply_does_not_mutate_original_state()
    {
        var original = ScoringHistoryState.Empty;
        _ = original.Apply(CreateEvent());
        Assert.Empty(original.Snapshots);
    }

    [Fact]
    public void ProcessCommand_produces_event_and_new_state()
    {
        var cmd = CreateCommand();
        var (state, evt) = ScoringHistoryState.Empty.ProcessCommand(cmd);

        Assert.Single(state.Snapshots);
        Assert.Equal(cmd.RequestId, evt.RequestId);
        Assert.Equal(cmd.Origin.Source, evt.Source);
        Assert.Equal(cmd.Origin.Query, evt.Query);
    }

    [Fact]
    public void Trim_removes_excess_by_count()
    {
        var state = ScoringHistoryState.Empty;
        for (var i = 0; i < 5; i++)
        {
            state = state.Apply(CreateEvent());
        }

        var trimmed = state.Trim(3, 365);
        Assert.Equal(3, trimmed.Snapshots.Count);
    }

    [Fact]
    public void Trim_removes_old_by_age()
    {
        var state = ScoringHistoryState.Empty
            .Apply(CreateEvent(timestamp: DateTimeOffset.UtcNow.AddDays(-10)))
            .Apply(CreateEvent(timestamp: DateTimeOffset.UtcNow));

        var trimmed = state.Trim(100, 5);
        Assert.Single(trimmed.Snapshots);
    }

    [Fact]
    public void QueryHistory_returns_paginated_newest_first()
    {
        var baseTime = DateTimeOffset.UtcNow.AddHours(-4);
        var state = ScoringHistoryState.Empty;
        for (var i = 0; i < 5; i++)
        {
            state = state.Apply(CreateEvent(timestamp: baseTime.AddHours(i)));
        }

        var result = state.QueryHistory(new QueryScoringHistory("test", 0, 3));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.Snapshots.Length);
        Assert.True(result.Snapshots[0].Timestamp > result.Snapshots[1].Timestamp);
    }

    [Fact]
    public void QueryDetail_returns_result_for_known_request()
    {
        var requestId = Guid.NewGuid();
        var state = ScoringHistoryState.Empty.Apply(CreateEvent(requestId: requestId));

        var result = state.QueryDetail(new QueryScoringDetail("test", requestId));
        Assert.IsType<ScoringDetailResult>(result);
        Assert.Equal(requestId, ((ScoringDetailResult)result).RequestId);
    }

    [Fact]
    public void QueryDetail_returns_not_found_for_unknown_request()
    {
        var state = ScoringHistoryState.Empty;
        var result = state.QueryDetail(new QueryScoringDetail("test", Guid.NewGuid()));
        Assert.IsType<ScoringDetailFailed>(result);
    }

    [Fact]
    public void ToScoringStats_empty_state_returns_nulls()
    {
        var result = ScoringHistoryState.Empty.ToScoringStats();

        Assert.Null(result.LastRun);
        Assert.Null(result.MatchRate);
    }

    [Fact]
    public void ToScoringStats_returns_last_run_and_match_rate()
    {
        var t1 = DateTimeOffset.UtcNow.AddHours(-2);
        var t2 = DateTimeOffset.UtcNow.AddHours(-1);

        var state = ScoringHistoryState.Empty
            .Apply(new ScoringRecorded(Guid.NewGuid(), "sonarr", "q", t1, 10, 8, []))
            .Apply(new ScoringRecorded(Guid.NewGuid(), "sonarr", "q", t2, 10, 6, []));

        var result = state.ToScoringStats();

        Assert.Equal(t2, result.LastRun);
        Assert.NotNull(result.MatchRate);
        Assert.Equal(0.7, result.MatchRate!.Value, 0.001);
    }

    [Fact]
    public void ToScoringStats_ignores_zero_candidate_snapshots_for_match_rate()
    {
        var state = ScoringHistoryState.Empty
            .Apply(new ScoringRecorded(Guid.NewGuid(), "sonarr", "q", DateTimeOffset.UtcNow, 0, 0, []))
            .Apply(new ScoringRecorded(Guid.NewGuid(), "sonarr", "q", DateTimeOffset.UtcNow, 10, 5, []));

        var result = state.ToScoringStats();

        Assert.NotNull(result.MatchRate);
        Assert.Equal(0.5, result.MatchRate!.Value, 0.001);
    }
}
