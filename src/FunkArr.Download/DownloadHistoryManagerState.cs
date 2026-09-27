using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Persistence.Events.Download;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Download;

public sealed record HistoryStats(
    int TotalCompleted,
    int TotalFailed,
    long TotalBytes,
    long TotalDownloadTimeSeconds)
{
    public static readonly HistoryStats Empty = new(0, 0, 0, 0);
}

public sealed record HistoryRecord(
    Guid DownloadId,
    DownloadCompletion Completion);

public sealed record DownloadHistoryManagerState(
    IReadOnlyList<HistoryRecord> Records,
    HashSet<Guid> Index,
    HistoryStats Stats)
{
    public static readonly DownloadHistoryManagerState Empty = new([], [], HistoryStats.Empty);
}

public static class DownloadHistoryManagerStateExtensions
{
    public static DownloadHistoryManagerState Apply(this DownloadHistoryManagerState state, DownloadHistoryRecorded evt)
    {
        var record = new HistoryRecord(evt.DownloadId, evt.Completion.ToDomain());

        var records = new List<HistoryRecord>(state.Records) { record };
        var index = new HashSet<Guid>(state.Index) { evt.DownloadId };
        var stats = AddToStats(state.Stats, record);

        return new(records, index, stats);
    }

    public static DownloadHistoryManagerState Apply(this DownloadHistoryManagerState state, HistoryRemoved evt)
    {
        var existing = state.Records.FirstOrDefault(r => r.DownloadId == evt.DownloadId);
        var records = state.Records.Where(r => r.DownloadId != evt.DownloadId).ToList();
        var index = new HashSet<Guid>(state.Index);
        index.Remove(evt.DownloadId);
        var stats = existing is not null ? RemoveFromStats(state.Stats, existing) : state.Stats;

        return new(records, index, stats);
    }

    public static DownloadHistoryManagerState Apply(this DownloadHistoryManagerState state, HistoryTrimmed evt)
    {
        if (evt.Count <= 0 || state.Records.Count == 0)
        {
            return state;
        }

        var trimCount = Math.Min(evt.Count, state.Records.Count);
        var trimmed = state.Records.Take(trimCount).ToArray();
        var remaining = state.Records.Skip(trimCount).ToList();
        var index = new HashSet<Guid>(remaining.Select(r => r.DownloadId));
        var stats = state.Stats;
        foreach (var record in trimmed)
        {
            stats = RemoveFromStats(stats, record);
        }

        return new(remaining, index, stats);
    }

    public static (DownloadHistoryManagerState State, int TrimCount) TrimIfNeeded(
        this DownloadHistoryManagerState state, int maxRecords)
    {
        if (maxRecords <= 0 || state.Records.Count <= maxRecords)
        {
            return (state, 0);
        }

        var trimCount = state.Records.Count - maxRecords;
        return (state.Apply(new HistoryTrimmed(trimCount)), trimCount);
    }

    public static bool Contains(this DownloadHistoryManagerState state, Guid downloadId) =>
        state.Index.Contains(downloadId);

    public static PersistedDownloadHistoryManagerState GetPersistenceState(this DownloadHistoryManagerState state) =>
        new([
            .. state.Records.Select(r => new DownloadHistoryRecorded(
                r.DownloadId, r.Completion.ToPersistence()))
        ]);

    public static DownloadHistoryManagerState FromPersistence(PersistedDownloadHistoryManagerState persisted)
    {
        var records = persisted.Records.Select(r => new HistoryRecord(
            r.DownloadId, r.Completion.ToDomain())).ToList();

        var index = new HashSet<Guid>(records.Select(r => r.DownloadId));
        var stats = HistoryStats.Empty;
        foreach (var record in records)
        {
            stats = AddToStats(stats, record);
        }

        return new(records, index, stats);
    }

    public static HistoryStatsResult ToHistoryStats(this DownloadHistoryManagerState state)
    {
        var s = state.Stats;
        var total = s.TotalCompleted + s.TotalFailed;
        if (total == 0)
        {
            return new HistoryStatsResult(0, 0, 0, 0, 0.0);
        }

        var avgTime = s.TotalCompleted > 0
            ? (int)(s.TotalDownloadTimeSeconds / s.TotalCompleted)
            : 0;
        var successRate = (double)s.TotalCompleted / total;

        return new HistoryStatsResult(s.TotalCompleted, s.TotalFailed, s.TotalBytes, avgTime, successRate);
    }

    public static HistoryCategoriesResult ToHistoryCategories(this DownloadHistoryManagerState state)
    {
        var categories = state.Records
            .Select(r => r.Completion.Category.ToString().ToLowerInvariant())
            .Distinct()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new HistoryCategoriesResult(categories);
    }

    public static HistoryResult ToHistoryResult(this DownloadHistoryManagerState state, QueryHistory query)
    {
        IEnumerable<HistoryRecord> filtered = state.Records;
        if (query.Category is not null)
        {
            filtered = filtered.Where(r => r.Completion.Category == query.Category);
        }

        var materialized = filtered.ToArray();
        var totalItems = materialized.Length;

        var paged = materialized.Skip(query.Start);
        if (query.Limit > 0)
        {
            paged = paged.Take(query.Limit);
        }

        return new HistoryResult(
            [.. paged.Select(r => new HistoryItem(r.DownloadId, r.Completion))],
            totalItems);
    }

    private static HistoryStats AddToStats(HistoryStats stats, HistoryRecord record) =>
        record.Completion.Status == DownloadStatus.Completed
            ? stats with
            {
                TotalCompleted = stats.TotalCompleted + 1,
                TotalBytes = stats.TotalBytes + record.Completion.Size,
                TotalDownloadTimeSeconds = stats.TotalDownloadTimeSeconds + record.Completion.DownloadTimeSeconds,
            }
            : stats with { TotalFailed = stats.TotalFailed + 1 };

    private static HistoryStats RemoveFromStats(HistoryStats stats, HistoryRecord record) =>
        record.Completion.Status == DownloadStatus.Completed
            ? stats with
            {
                TotalCompleted = stats.TotalCompleted - 1,
                TotalBytes = stats.TotalBytes - record.Completion.Size,
                TotalDownloadTimeSeconds = stats.TotalDownloadTimeSeconds - record.Completion.DownloadTimeSeconds,
            }
            : stats with { TotalFailed = stats.TotalFailed - 1 };
}
