using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Manager;

public sealed record ConcurrencyLimit(int Limit);

public sealed record ScheduleChange(bool Enabled, DateTimeOffset? NextWindow);

public sealed class DownloadManagerState
{
    public List<QueueEntry> Queued { get; private set; } = [];
    public Dictionary<Guid, DispatchedEntry> Dispatched { get; private set; } = [];
    public bool Paused { get; private set; }
    public bool ScheduleEnabled { get; private set; } = true;
    public DateTimeOffset? NextWindow { get; private set; }
    public int MaxConcurrent { get; private set; }

    public static DownloadManagerState Empty => new();

    public static DownloadManagerState FromPersistence(PersistedDownloadManagerState persisted)
    {
        var state = new DownloadManagerState
        {
            Queued =
            [
                .. persisted.Queued.Select(e =>
                    new QueueEntry(e.DownloadId, e.Priority.ToDomain(), e.Category.ToDomain()))
            ],
            Dispatched = persisted.Dispatched.ToDictionary(
                e => e.DownloadId,
                e => new DispatchedEntry(e.Priority.ToDomain(), e.Category.ToDomain())),
            Paused = persisted.Paused
        };
        return state;
    }

    public void Apply(DownloadEnqueued evt)
    {
        var priority = evt.Priority.ToDomain();
        var category = (evt.Category ?? PersistedMediaType.Show).ToDomain();
        var entry = new QueueEntry(evt.DownloadId, priority, category);
        var insertAt = FindBucketEnd(Queued, priority);
        Queued.Insert(insertAt, entry);
    }

    public void Apply(DownloadDispatched evt)
    {
        var entry = Queued.First(e => e.Id == evt.DownloadId);
        Dispatched[evt.DownloadId] = new(entry.Priority, entry.Category);
        Queued.RemoveAll(e => e.Id == evt.DownloadId);
    }

    public void Apply(DownloadDequeued evt)
    {
        Dispatched.Remove(evt.DownloadId);
        Queued.RemoveAll(e => e.Id == evt.DownloadId);
    }

    public void ResetDispatched()
    {
        var resetEntries = Dispatched
            .Select(kv => new QueueEntry(kv.Key, kv.Value.Priority, kv.Value.Category));
        var merged = new List<QueueEntry>(resetEntries);
        merged.AddRange(Queued);
        merged.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        Queued = merged;
        Dispatched = [];
    }

    public void Apply(DownloadMoved evt)
    {
        var idx = Queued.FindIndex(e => e.Id == evt.DownloadId);
        if (idx < 0)
        {
            return;
        }

        var entry = Queued[idx];
        Queued.RemoveAt(idx);

        var (bucketStart, bucketEnd) = FindBucketBounds(Queued, entry.Priority);
        var target = Math.Clamp(evt.Position, bucketStart, bucketEnd);
        Queued.Insert(target, entry);
    }

    public void Apply(DownloadSwapped evt)
    {
        var idx1 = Queued.FindIndex(e => e.Id == evt.DownloadId1);
        var idx2 = Queued.FindIndex(e => e.Id == evt.DownloadId2);
        if (idx1 < 0 || idx2 < 0)
        {
            return;
        }

        (Queued[idx1], Queued[idx2]) = (Queued[idx2], Queued[idx1]);
    }

    public void Apply(DownloadPriorityChanged evt)
    {
        var idx = Queued.FindIndex(e => e.Id == evt.DownloadId);
        if (idx < 0)
        {
            return;
        }

        var priority = evt.Priority.ToDomain();
        var category = Queued[idx].Category;
        Queued.RemoveAt(idx);

        var newEntry = new QueueEntry(evt.DownloadId, priority, category);
        var insertAt = FindBucketEnd(Queued, priority);
        Queued.Insert(insertAt, newEntry);
    }

    public void Apply(DownloadsPaused _) => Paused = true;

    public void Apply(DownloadsResumed _) => Paused = false;

    public void Apply(ConcurrencyLimit limit) => MaxConcurrent = limit.Limit;

    public void Apply(ScheduleChange change)
    {
        ScheduleEnabled = change.Enabled;
        NextWindow = change.Enabled ? null : change.NextWindow;
    }

    public bool Contains(Guid downloadId)
        => Queued.Any(e => e.Id == downloadId) || Dispatched.ContainsKey(downloadId);

    public (Guid[] PageIds, int TotalItems) GetPage(QueryQueue query)
    {
        IEnumerable<Guid> ids = Dispatched.Keys
            .Concat(Queued.Select(e => e.Id));

        if (query.Category is { } category)
        {
            ids = Dispatched
                .Where(kv => kv.Value.Category == category)
                .Select(kv => kv.Key)
                .Concat(Queued
                    .Where(e => e.Category == category)
                    .Select(e => e.Id));
        }

        var allIds = ids.ToArray();
        var totalItems = allIds.Length;

        var paged = allIds.Skip(query.Start);
        if (query.Limit > 0)
        {
            paged = paged.Take(query.Limit);
        }

        return ([.. paged], totalItems);
    }

    public PersistedDownloadManagerState GetPersistenceState() =>
        new(
            [
                .. Queued.Select(e =>
                    new PersistedQueueEntry(e.Id, e.Priority.ToPersistence(), e.Category.ToPersistence()))
            ],
            [
                .. Dispatched.Select(kv => new PersistedDispatchedEntry(kv.Key, kv.Value.Priority.ToPersistence(),
                    kv.Value.Category.ToPersistence()))
            ],
            Paused);

    private static int FindBucketEnd(List<QueueEntry> list, DownloadPriority priority)
    {
        var lastIdx = -1;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Priority >= priority)
            {
                lastIdx = i;
            }
        }

        return lastIdx + 1;
    }

    private static (int Start, int End) FindBucketBounds(List<QueueEntry> list, DownloadPriority priority)
    {
        var start = -1;
        var end = -1;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Priority == priority)
            {
                if (start < 0)
                {
                    start = i;
                }

                end = i + 1;
            }
        }

        if (start < 0)
        {
            return (FindBucketEnd(list, priority), FindBucketEnd(list, priority));
        }

        return (start, end);
    }
}
