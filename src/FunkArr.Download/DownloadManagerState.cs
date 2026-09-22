using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed record DownloadManagerState(
    IReadOnlyList<QueueEntry> Queued,
    IReadOnlyDictionary<Guid, DownloadPriority> Dispatched,
    bool Paused = false,
    bool ScheduleEnabled = true,
    DateTimeOffset? NextWindow = null)
{
    public static readonly DownloadManagerState Empty =
        new([], new Dictionary<Guid, DownloadPriority>());
}

public static class DownloadManagerStateExtensions
{
    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadEnqueued evt)
    {
        var priority = evt.Priority.ToDomain();
        var entry = new QueueEntry(evt.DownloadId, priority);
        var list = state.Queued.ToList();
        var insertAt = FindBucketEnd(list, priority);
        list.Insert(insertAt, entry);
        return state with { Queued = list };
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadDispatched evt)
    {
        var entry = state.Queued.First(e => e.Id == evt.DownloadId);
        var dict = new Dictionary<Guid, DownloadPriority>(state.Dispatched)
        {
            [evt.DownloadId] = entry.Priority,
        };
        return new(
            Queued: state.Queued.Where(e => e.Id != evt.DownloadId).ToArray(),
            Dispatched: dict);
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadDequeued evt)
    {
        var dict = new Dictionary<Guid, DownloadPriority>(state.Dispatched);
        dict.Remove(evt.DownloadId);
        return new(
            Queued: state.Queued.Where(e => e.Id != evt.DownloadId).ToArray(),
            Dispatched: dict);
    }

    public static DownloadManagerState ResetDispatched(this DownloadManagerState state)
    {
        var resetEntries = state.Dispatched
            .Select(kv => new QueueEntry(kv.Key, kv.Value));
        var merged = new List<QueueEntry>(resetEntries);
        merged.AddRange(state.Queued);
        merged.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return new(Queued: merged, Dispatched: new Dictionary<Guid, DownloadPriority>());
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadMoved evt)
    {
        var idx = state.Queued.ToList().FindIndex(e => e.Id == evt.DownloadId);
        if (idx < 0) return state;

        var entry = state.Queued[idx];
        var list = state.Queued.ToList();
        list.RemoveAt(idx);

        var (bucketStart, bucketEnd) = FindBucketBounds(list, entry.Priority);
        var target = Math.Clamp(evt.Position, bucketStart, bucketEnd);
        list.Insert(target, entry);
        return state with { Queued = list };
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadSwapped evt)
    {
        var list = state.Queued.ToList();
        var idx1 = list.FindIndex(e => e.Id == evt.DownloadId1);
        var idx2 = list.FindIndex(e => e.Id == evt.DownloadId2);
        if (idx1 < 0 || idx2 < 0) return state;

        (list[idx1], list[idx2]) = (list[idx2], list[idx1]);
        return state with { Queued = list };
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadPriorityChanged evt)
    {
        var idx = state.Queued.ToList().FindIndex(e => e.Id == evt.DownloadId);
        if (idx < 0) return state;

        var priority = evt.Priority.ToDomain();
        var list = state.Queued.ToList();
        list.RemoveAt(idx);

        var newEntry = new QueueEntry(evt.DownloadId, priority);
        var insertAt = FindBucketEnd(list, priority);
        list.Insert(insertAt, newEntry);
        return state with { Queued = list };
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadsPaused _)
        => state with { Paused = true };

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadsResumed _)
        => state with { Paused = false };

    public static bool Contains(this DownloadManagerState state, Guid downloadId)
        => state.Queued.Any(e => e.Id == downloadId) || state.Dispatched.ContainsKey(downloadId);

    public static QueueResult PaginateQueue(QueueItem[] items, QueryQueue query, int totalSlots, DownloadManagerState state)
    {
        IEnumerable<QueueItem> filtered = items;
        if (query.Category is not null)
        {
            filtered = filtered
                .Where(i => i.Category == query.Category);
        }

        var materialized = filtered.ToArray();
        var totalItems = materialized.Length;

        var paged = materialized.Skip(query.Start);
        if (query.Limit > 0)
        {
            paged = paged.Take(query.Limit);
        }

        return new QueueResult(paged.ToArray(), totalSlots, totalItems,
            state.Paused, state.ScheduleEnabled, state.NextWindow);
    }

    private static int FindBucketEnd(List<QueueEntry> list, DownloadPriority priority)
    {
        var lastIdx = -1;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Priority >= priority)
                lastIdx = i;
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
                if (start < 0) start = i;
                end = i + 1;
            }
        }

        if (start < 0)
            return (FindBucketEnd(list, priority), FindBucketEnd(list, priority));

        return (start, end);
    }
}
