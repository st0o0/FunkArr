using System.Collections.Immutable;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed record DownloadManagerState(
    IReadOnlyList<QueueEntry> Queued,
    IReadOnlyDictionary<Guid, DispatchedEntry> Dispatched,
    bool Paused = false,
    bool ScheduleEnabled = true,
    DateTimeOffset? NextWindow = null)
{
    public static readonly DownloadManagerState Empty =
        new([], new Dictionary<Guid, DispatchedEntry>());

    public static DownloadManagerState FromPersistence(PersistedDownloadManagerState persisted) =>
        new(
            Queued: persisted.Queued.Select(e => new QueueEntry(e.DownloadId, e.Priority.ToDomain(), e.Category.ToDomain())).ToArray(),
            Dispatched: persisted.Dispatched.ToImmutableDictionary(
                e => e.DownloadId,
                e => new DispatchedEntry(e.Priority.ToDomain(), e.Category.ToDomain())),
            Paused: persisted.Paused);
}

public static class DownloadManagerStateExtensions
{
    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadEnqueued evt)
    {
        var priority = evt.Priority.ToDomain();
        var category = (evt.Category ?? PersistedMediaType.Show).ToDomain();
        var entry = new QueueEntry(evt.DownloadId, priority, category);
        var list = state.Queued.ToList();
        var insertAt = FindBucketEnd(list, priority);
        list.Insert(insertAt, entry);
        return state with { Queued = list };
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadDispatched evt)
    {
        var entry = state.Queued.First(e => e.Id == evt.DownloadId);
        var dict = new Dictionary<Guid, DispatchedEntry>(state.Dispatched)
        {
            [evt.DownloadId] = new(entry.Priority, entry.Category),
        };
        return new(
            Queued: state.Queued.Where(e => e.Id != evt.DownloadId).ToArray(),
            Dispatched: dict);
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadDequeued evt)
    {
        var dict = new Dictionary<Guid, DispatchedEntry>(state.Dispatched);
        dict.Remove(evt.DownloadId);
        return new(
            Queued: state.Queued.Where(e => e.Id != evt.DownloadId).ToArray(),
            Dispatched: dict);
    }

    public static DownloadManagerState ResetDispatched(this DownloadManagerState state)
    {
        var resetEntries = state.Dispatched
            .Select(kv => new QueueEntry(kv.Key, kv.Value.Priority, kv.Value.Category));
        var merged = new List<QueueEntry>(resetEntries);
        merged.AddRange(state.Queued);
        merged.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return new(Queued: merged, Dispatched: new Dictionary<Guid, DispatchedEntry>());
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadMoved evt)
    {
        var idx = state.Queued.ToList().FindIndex(e => e.Id == evt.DownloadId);
        if (idx < 0)
        {
            return state;
        }

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
        if (idx1 < 0 || idx2 < 0)
        {
            return state;
        }

        (list[idx1], list[idx2]) = (list[idx2], list[idx1]);
        return state with { Queued = list };
    }

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadPriorityChanged evt)
    {
        var idx = state.Queued.ToList().FindIndex(e => e.Id == evt.DownloadId);
        if (idx < 0)
        {
            return state;
        }

        var priority = evt.Priority.ToDomain();
        var category = state.Queued[idx].Category;
        var list = state.Queued.ToList();
        list.RemoveAt(idx);

        var newEntry = new QueueEntry(evt.DownloadId, priority, category);
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

    public static (Guid[] PageIds, int TotalItems) GetPage(this DownloadManagerState state, QueryQueue query)
    {
        var ids = state.Dispatched.Keys
            .Concat(state.Queued.Select(e => e.Id));

        if (query.Category is { } category)
        {
            ids = state.Dispatched
                .Where(kv => kv.Value.Category == category)
                .Select(kv => kv.Key)
                .Concat(state.Queued
                    .Where(e => e.Category == category)
                    .Select(e => e.Id));
        }

        var allIds = ids.ToArray();
        var totalItems = allIds.Length;

        var paged = allIds.AsEnumerable().Skip(query.Start);
        if (query.Limit > 0)
        {
            paged = paged.Take(query.Limit);
        }

        return (paged.ToArray(), totalItems);
    }

    public static PersistedDownloadManagerState GetPersistenceState(this DownloadManagerState state) =>
        new(
            state.Queued.Select(e => new PersistedQueueEntry(e.Id, e.Priority.ToPersistence(), e.Category.ToPersistence())).ToArray(),
            state.Dispatched.Select(kv => new PersistedDispatchedEntry(kv.Key, kv.Value.Priority.ToPersistence(), kv.Value.Category.ToPersistence())).ToArray(),
            state.Paused);



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
