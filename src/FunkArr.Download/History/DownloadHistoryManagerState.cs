using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.History;

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

public sealed class DownloadHistoryManagerState
{
    public List<HistoryRecord> Records { get; private set; } = [];
    public HashSet<Guid> Index { get; private set; } = [];
    public HistoryStats Stats { get; private set; } = HistoryStats.Empty;

    public static DownloadHistoryManagerState Empty => new();

    public void Apply(DownloadHistoryRecorded evt)
    {
        var record = new HistoryRecord(evt.DownloadId, evt.Completion.ToDomain());
        Records.Add(record);
        Index.Add(evt.DownloadId);
        Stats = AddToStats(Stats, record);
    }

    public void Apply(HistoryRemoved evt)
    {
        var existing = Records.FirstOrDefault(r => r.DownloadId == evt.DownloadId);
        Records.RemoveAll(r => r.DownloadId == evt.DownloadId);
        Index.Remove(evt.DownloadId);
        if (existing is not null)
        {
            Stats = RemoveFromStats(Stats, existing);
        }
    }

    public void Apply(HistoryTrimmed evt)
    {
        if (evt.Count <= 0 || Records.Count == 0)
        {
            return;
        }

        var trimCount = Math.Min(evt.Count, Records.Count);
        var trimmed = Records.Take(trimCount).ToArray();
        Records.RemoveRange(0, trimCount);
        Index = [.. Records.Select(r => r.DownloadId)];
        foreach (var record in trimmed)
        {
            Stats = RemoveFromStats(Stats, record);
        }
    }

    public int TrimIfNeeded(int maxRecords)
    {
        if (maxRecords <= 0 || Records.Count <= maxRecords)
        {
            return 0;
        }

        var trimCount = Records.Count - maxRecords;
        Apply(new HistoryTrimmed(trimCount));
        return trimCount;
    }

    public bool Contains(Guid downloadId) => Index.Contains(downloadId);

    public HistoryItem? FindById(Guid downloadId)
    {
        if (!Index.Contains(downloadId))
        {
            return null;
        }

        var record = Records.FirstOrDefault(r => r.DownloadId == downloadId);
        return record is not null ? new HistoryItem(record.DownloadId, record.Completion) : null;
    }

    public PersistedDownloadHistoryManagerState GetPersistenceState() =>
        new([
            .. Records.Select(r => new DownloadHistoryRecorded(
                r.DownloadId, r.Completion.ToPersistence()))
        ]);

    public static DownloadHistoryManagerState FromPersistence(PersistedDownloadHistoryManagerState persisted)
    {
        var state = new DownloadHistoryManagerState();
        foreach (var r in persisted.Records)
        {
            state.Apply(r);
        }

        return state;
    }

    public HistoryStatsResult ToHistoryStats()
    {
        var s = Stats;
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

    public HistoryCategoriesResult ToHistoryCategories()
    {
        var categories = Records
            .Select(r => r.Completion.Category.ToString().ToLowerInvariant())
            .Distinct()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new HistoryCategoriesResult(categories);
    }

    public HistoryResult ToHistoryResult(QueryHistory query)
    {
        IEnumerable<HistoryRecord> filtered = Records;
        if (query.Category is not null)
        {
            filtered = filtered.Where(r => r.Completion.Category == query.Category);
        }

        var materialized = filtered.ToArray();
        var totalItems = materialized.Length;

        var paged = materialized.Reverse().Skip(query.Start);
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
                TotalDownloadTimeSeconds = stats.TotalDownloadTimeSeconds + record.Completion.DownloadTimeSeconds
            }
            : stats with { TotalFailed = stats.TotalFailed + 1 };

    private static HistoryStats RemoveFromStats(HistoryStats stats, HistoryRecord record) =>
        record.Completion.Status == DownloadStatus.Completed
            ? stats with
            {
                TotalCompleted = stats.TotalCompleted - 1,
                TotalBytes = stats.TotalBytes - record.Completion.Size,
                TotalDownloadTimeSeconds = stats.TotalDownloadTimeSeconds - record.Completion.DownloadTimeSeconds
            }
            : stats with { TotalFailed = stats.TotalFailed - 1 };
}
