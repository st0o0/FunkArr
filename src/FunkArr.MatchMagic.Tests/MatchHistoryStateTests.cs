using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.MatchHistory;

namespace FunkArr.MatchMagic.Tests;

public sealed class MatchHistoryStateTests
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

    private static RecordScoringResult CreateCommand(string ruleSetId = "test") => new(
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
        Assert.Empty(MatchHistoryState.Empty.Snapshots);
    }

    [Fact]
    public void Apply_adds_snapshot()
    {
        var state = MatchHistoryState.Empty.Apply(CreateEvent());
        Assert.Single(state.Snapshots);
    }

    [Fact]
    public void Apply_preserves_event_data()
    {
        var evt = CreateEvent(requestId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var state = MatchHistoryState.Empty.Apply(evt);

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
        var original = MatchHistoryState.Empty;
        _ = original.Apply(CreateEvent());
        Assert.Empty(original.Snapshots);
    }

    [Fact]
    public void ProcessCommand_produces_event_and_new_state()
    {
        var cmd = CreateCommand();
        var (state, evt) = MatchHistoryState.Empty.ProcessCommand(cmd);

        Assert.Single(state.Snapshots);
        Assert.Equal(cmd.RequestId, evt.RequestId);
        Assert.Equal(cmd.Origin.Source, evt.Source);
        Assert.Equal(cmd.Origin.Query, evt.Query);
    }

    [Fact]
    public void Trim_removes_excess_by_count()
    {
        var state = MatchHistoryState.Empty;
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
        var state = MatchHistoryState.Empty
            .Apply(CreateEvent(timestamp: DateTimeOffset.UtcNow.AddDays(-10)))
            .Apply(CreateEvent(timestamp: DateTimeOffset.UtcNow));

        var trimmed = state.Trim(100, 5);
        Assert.Single(trimmed.Snapshots);
    }

    [Fact]
    public void QueryHistory_returns_paginated_newest_first()
    {
        var baseTime = DateTimeOffset.UtcNow.AddHours(-4);
        var state = MatchHistoryState.Empty;
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
        var state = MatchHistoryState.Empty.Apply(CreateEvent(requestId: requestId));

        var result = state.QueryDetail(new QueryScoringDetail("test", requestId));
        Assert.IsType<ScoringDetailResult>(result);
        Assert.Equal(requestId, ((ScoringDetailResult)result).RequestId);
    }

    [Fact]
    public void QueryDetail_returns_not_found_for_unknown_request()
    {
        var state = MatchHistoryState.Empty;
        var result = state.QueryDetail(new QueryScoringDetail("test", Guid.NewGuid()));
        Assert.IsType<ScoringDetailNotFound>(result);
    }
}
