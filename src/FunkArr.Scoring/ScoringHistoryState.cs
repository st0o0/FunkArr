using System.Collections.Immutable;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.ScoringHistory;

namespace FunkArr.Scoring;

public sealed record ScoringHistoryState(ImmutableList<ScoringHistoryState.ScoringSnapshot> Snapshots)
{
    public static readonly ScoringHistoryState Empty = new(ImmutableList<ScoringSnapshot>.Empty);

    public sealed record ScoringSnapshot(
        Guid RequestId,
        ScoringOrigin Origin,
        DateTimeOffset Timestamp,
        int CandidateCount,
        int MatchedCount,
        ItemTrace[] ItemTraces);
}

public static class ScoringHistoryStateExtensions
{
    public static (ScoringHistoryState State, ScoringRecorded Event) ProcessCommand(
        this ScoringHistoryState state, RecordScoring cmd)
    {
        var evt = new ScoringRecorded(
            cmd.RequestId,
            cmd.Origin.Source,
            cmd.Origin.Query,
            cmd.Timestamp,
            cmd.CandidateCount,
            cmd.MatchedCount,
            cmd.ItemTraces);

        return (state.Apply(evt), evt);
    }

    public static ScoringHistoryState Apply(this ScoringHistoryState state, ScoringRecorded evt)
    {
        var snapshot = new ScoringHistoryState.ScoringSnapshot(
            evt.RequestId,
            new ScoringOrigin(evt.Source, evt.Query),
            evt.Timestamp,
            evt.CandidateCount,
            evt.MatchedCount,
            evt.ItemTraces);

        return new ScoringHistoryState(Snapshots: state.Snapshots.Add(snapshot));
    }

    public static ScoringHistoryState Trim(this ScoringHistoryState state, int maxSnapshots, int maxAgeDays)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-maxAgeDays);
        var trimmed = state.Snapshots
            .Where(s => s.Timestamp >= cutoff)
            .ToImmutableList();

        if (trimmed.Count > maxSnapshots)
        {
            trimmed = trimmed.Skip(trimmed.Count - maxSnapshots).ToImmutableList();
        }

        return new ScoringHistoryState(Snapshots: trimmed);
    }

    public static ScoringHistoryResult QueryHistory(
        this ScoringHistoryState state, QueryScoringHistory query)
    {
        var total = state.Snapshots.Count;
        var page = state.Snapshots
            .Reverse()
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(s => new ScoringSnapshotSummary(
                s.RequestId,
                s.Origin.Source,
                s.Origin.Query,
                s.Timestamp,
                s.CandidateCount,
                s.MatchedCount))
            .ToArray();

        return new ScoringHistoryResult(query.RuleSetId, total, page);
    }

    public static ScoringStatsResult ToScoringStats(this ScoringHistoryState state)
    {
        if (state.Snapshots.Count == 0)
        {
            return new ScoringStatsResult(null, null);
        }

        var lastRun = state.Snapshots[^1].Timestamp;

        var withCandidates = state.Snapshots.Where(s => s.CandidateCount > 0).ToArray();
        double? matchRate = withCandidates.Length > 0
            ? withCandidates.Average(s => (double)s.MatchedCount / s.CandidateCount)
            : null;

        return new ScoringStatsResult(lastRun, matchRate);
    }

    public static ScoringDetailResponse QueryDetail(this ScoringHistoryState state, QueryScoringDetail query)
    {
        var snapshot = state.Snapshots.FirstOrDefault(s => s.RequestId == query.RequestId);
        if (snapshot is null)
        {
            return new ScoringDetailFailed(new KeyNotFoundException($"Scoring detail {query.RequestId} not found"));
        }

        return new ScoringDetailResult(
            snapshot.RequestId,
            snapshot.Origin.Source,
            snapshot.Origin.Query,
            snapshot.Timestamp,
            snapshot.ItemTraces);
    }
}
