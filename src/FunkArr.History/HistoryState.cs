using System.Collections.Immutable;
using FunkArr.Messages.History;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.ScoringHistory;

namespace FunkArr.History;

public sealed record HistoryState(
    ImmutableList<HistoryState.HistorySnapshot> Snapshots,
    ScoringStatsResult Stats)
{
    public static readonly HistoryState Empty = new(
        ImmutableList<HistorySnapshot>.Empty,
        new ScoringStatsResult(null, null));

    public sealed record HistorySnapshot(
        Guid RequestId,
        ScoringOrigin Origin,
        DateTimeOffset Timestamp,
        int CandidateCount,
        int MatchedCount,
        int EnrichedCount,
        ItemTrace[] ItemTraces);

    public static HistoryState FromPersistence(PersistedHistoryState persisted)
    {
        var snapshots = persisted.Entries
            .Select(e => new HistorySnapshot(
                e.RequestId,
                new ScoringOrigin(e.Source, e.Query),
                e.Timestamp,
                e.CandidateCount,
                e.MatchedCount,
                e.EnrichedCount,
                e.ItemTraces))
            .ToImmutableList();

        return new HistoryState(snapshots, ComputeStats(snapshots));
    }

    internal static ScoringStatsResult ComputeStats(ImmutableList<HistorySnapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return new ScoringStatsResult(null, null);
        }

        var lastRun = snapshots[^1].Timestamp;

        var withCandidates = snapshots.Where(s => s.CandidateCount > 0).ToArray();
        double? matchRate = withCandidates.Length > 0
            ? withCandidates.Average(s => (double)s.MatchedCount / s.CandidateCount)
            : null;

        var withMatched = snapshots.Where(s => s.MatchedCount > 0).ToArray();
        double? enrichmentRate = withMatched.Length > 0
            ? withMatched.Average(s => (double)s.EnrichedCount / s.MatchedCount)
            : null;

        return new ScoringStatsResult(lastRun, matchRate, enrichmentRate, snapshots.Count);
    }
}

public static class HistoryStateExtensions
{
    public static (HistoryState State, HistoryRecorded Event) ProcessCommand(
        this HistoryState state, RecordHistory cmd)
    {
        var evt = new HistoryRecorded(
            cmd.RequestId,
            cmd.Origin.Source,
            cmd.Origin.Query,
            cmd.Timestamp,
            cmd.CandidateCount,
            cmd.MatchedCount,
            cmd.EnrichedCount,
            cmd.ItemTraces);

        return (state.Apply(evt), evt);
    }

    public static HistoryState Apply(this HistoryState state, HistoryRecorded evt)
    {
        var snapshot = new HistoryState.HistorySnapshot(
            evt.RequestId,
            new ScoringOrigin(evt.Source, evt.Query),
            evt.Timestamp,
            evt.CandidateCount,
            evt.MatchedCount,
            evt.EnrichedCount,
            evt.ItemTraces);

        var snapshots = state.Snapshots.Add(snapshot);
        var stats = HistoryState.ComputeStats(snapshots);

        return new HistoryState(snapshots, stats);
    }

    public static HistoryState Trim(this HistoryState state, int maxSnapshots, int maxAgeDays)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-maxAgeDays);
        var trimmed = state.Snapshots
            .Where(s => s.Timestamp >= cutoff)
            .ToImmutableList();

        if (trimmed.Count > maxSnapshots)
        {
            trimmed = trimmed.Skip(trimmed.Count - maxSnapshots).ToImmutableList();
        }

        if (trimmed.Count == state.Snapshots.Count)
        {
            return state;
        }

        return new HistoryState(trimmed, HistoryState.ComputeStats(trimmed));
    }

    public static ScoringHistoryResult QueryHistory(
        this HistoryState state, QueryScoringHistory query)
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

    public static ScoringDetailResponse QueryDetail(this HistoryState state, QueryScoringDetail query)
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

    public static PersistedHistoryState GetSnapshot(this HistoryState state) =>
        state.GetPersistenceState();

    public static PersistedHistoryState GetPersistenceState(this HistoryState state) =>
        new(state.Snapshots
            .Select(s => new HistoryRecorded(
                s.RequestId,
                s.Origin.Source,
                s.Origin.Query,
                s.Timestamp,
                s.CandidateCount,
                s.MatchedCount,
                s.EnrichedCount,
                s.ItemTraces))
            .ToArray());
}
