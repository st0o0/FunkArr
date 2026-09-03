using System.Collections.Immutable;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.MatchHistory;

namespace FunkArr.MatchMagic;

public sealed record MatchHistoryState(ImmutableList<MatchHistoryState.ScoringSnapshot> Snapshots)
{
    public static readonly MatchHistoryState Empty = new(ImmutableList<ScoringSnapshot>.Empty);

    public sealed record ScoringSnapshot(
        Guid RequestId,
        ScoringOrigin Origin,
        DateTimeOffset Timestamp,
        int CandidateCount,
        int MatchedCount,
        ItemTrace[] ItemTraces);
}

public static class MatchHistoryStateExtensions
{
    public static (MatchHistoryState State, ScoringRecorded Event) ProcessCommand(
        this MatchHistoryState state, RecordScoringResult cmd)
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

    public static MatchHistoryState Apply(this MatchHistoryState state, ScoringRecorded evt)
    {
        var snapshot = new MatchHistoryState.ScoringSnapshot(
            evt.RequestId,
            new ScoringOrigin(evt.Source, evt.Query),
            evt.Timestamp,
            evt.CandidateCount,
            evt.MatchedCount,
            evt.ItemTraces);

        return new MatchHistoryState(Snapshots: state.Snapshots.Add(snapshot));
    }

    public static MatchHistoryState Trim(this MatchHistoryState state, int maxSnapshots, int maxAgeDays)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-maxAgeDays);
        var trimmed = state.Snapshots
            .Where(s => s.Timestamp >= cutoff)
            .ToImmutableList();

        if (trimmed.Count > maxSnapshots)
        {
            trimmed = trimmed.Skip(trimmed.Count - maxSnapshots).ToImmutableList();
        }

        return new MatchHistoryState(Snapshots: trimmed);
    }

    public static ScoringHistoryResult QueryHistory(
        this MatchHistoryState state, QueryScoringHistory query)
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

    public static object QueryDetail(this MatchHistoryState state, QueryScoringDetail query)
    {
        var snapshot = state.Snapshots.FirstOrDefault(s => s.RequestId == query.RequestId);
        if (snapshot is null)
        {
            return new ScoringDetailNotFound(query.RequestId);
        }

        return new ScoringDetailResult(
            snapshot.RequestId,
            snapshot.Origin.Source,
            snapshot.Origin.Query,
            snapshot.Timestamp,
            snapshot.ItemTraces);
    }
}
