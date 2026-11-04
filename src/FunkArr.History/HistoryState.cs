using FunkArr.Messages.History;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.ScoringHistory;

namespace FunkArr.History;

public sealed class HistoryState
{
    public List<HistoryEntry> Snapshots { get; } = [];

    public ScoringStatsResult Stats { get; private set; } = new(null, null);

    public static HistoryState Empty => new();

    public sealed record HistoryEntry(
        Guid RequestId,
        ScoringOrigin Origin,
        DateTimeOffset Timestamp,
        int CandidateCount,
        int MatchedCount,
        int EnrichedCount,
        ItemTrace[] ItemTraces);

    public static HistoryState FromPersistence(PersistedHistoryState persisted)
    {
        var state = new HistoryState();
        state.Snapshots.AddRange(persisted.Entries
            .Select(e => new HistoryEntry(
                e.RequestId,
                new ScoringOrigin(e.Source.ToDomain(), e.Query),
                e.Timestamp,
                e.CandidateCount,
                e.MatchedCount,
                e.EnrichedCount,
                [.. e.ItemTraces.Select(t => t.ToDomain())])));
        state.Stats = ComputeStats(state.Snapshots);

        return state;
    }

    internal static ScoringStatsResult ComputeStats(IReadOnlyList<HistoryEntry> snapshots)
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

    public ScoringHistoryRecorded ProcessCommand(RecordHistory cmd) =>
        new(
            cmd.RequestId,
            cmd.Origin.Source.ToPersistence(),
            cmd.Origin.Query,
            cmd.Timestamp,
            cmd.CandidateCount,
            cmd.MatchedCount,
            cmd.EnrichedCount,
            [.. cmd.ItemTraces.Select(t => t.ToPersistence())]);

    public void Apply(ScoringHistoryRecorded evt)
    {
        Snapshots.Add(new HistoryEntry(
            evt.RequestId,
            new ScoringOrigin(evt.Source.ToDomain(), evt.Query),
            evt.Timestamp,
            evt.CandidateCount,
            evt.MatchedCount,
            evt.EnrichedCount,
            [.. evt.ItemTraces.Select(t => t.ToDomain())]));
        Stats = ComputeStats(Snapshots);
    }

    public void Trim(int maxSnapshots, int maxAgeDays)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-maxAgeDays);
        var trimmed = Snapshots.Where(s => s.Timestamp >= cutoff).ToList();

        if (trimmed.Count > maxSnapshots)
        {
            trimmed = [.. trimmed.Skip(trimmed.Count - maxSnapshots)];
        }

        if (trimmed.Count == Snapshots.Count)
        {
            return;
        }

        Snapshots.Clear();
        Snapshots.AddRange(trimmed);
        Stats = ComputeStats(Snapshots);
    }

    public ScoringHistoryResult QueryHistory(QueryScoringHistory query)
    {
        var total = Snapshots.Count;
        var page = Enumerable.Reverse(Snapshots)
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

        return new ScoringHistoryResult(query.HistoryId, total, page);
    }

    public ScoringDetailResponse QueryDetail(QueryScoringDetail query)
    {
        var snapshot = Snapshots.FirstOrDefault(s => s.RequestId == query.RequestId);
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

    public PersistedHistoryState GetSnapshot() => GetPersistenceState();

    public PersistedHistoryState GetPersistenceState() =>
        new([
            .. Snapshots
                .Select(s => new ScoringHistoryRecorded(
                    s.RequestId,
                    s.Origin.Source.ToPersistence(),
                    s.Origin.Query,
                    s.Timestamp,
                    s.CandidateCount,
                    s.MatchedCount,
                    s.EnrichedCount,
                    [.. s.ItemTraces.Select(t => t.ToPersistence())]))
        ]);
}
