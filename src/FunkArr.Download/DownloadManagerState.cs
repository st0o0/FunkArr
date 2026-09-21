using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed record DownloadManagerSnapshot(Guid[] Queued, Guid[] Dispatched);

public sealed record DownloadManagerState(
    IReadOnlyList<Guid> Queued,
    IReadOnlySet<Guid> Dispatched)
{
    public static readonly DownloadManagerState Empty = new([], new HashSet<Guid>());

    public static DownloadManagerState FromSnapshot(DownloadManagerSnapshot snapshot) =>
        new(snapshot.Queued.ToList(), snapshot.Dispatched.ToHashSet());
}

public static class DownloadManagerStateExtensions
{
    public static DownloadManagerSnapshot GetSnapshot(this DownloadManagerState state) =>
        new(state.Queued.ToArray(), state.Dispatched.ToArray());

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadEnqueued evt)
        => state with { Queued = [.. state.Queued, evt.DownloadId] };

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadDispatched evt)
        => new(Queued: state.Queued.Where(id => id != evt.DownloadId).ToArray(),
            Dispatched: state.Dispatched.Append(evt.DownloadId).ToHashSet());

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadDequeued evt)
        => new(Queued: state.Queued.Where(id => id != evt.DownloadId).ToArray(),
            Dispatched: state.Dispatched.Where(id => id != evt.DownloadId).ToHashSet());

    public static DownloadManagerState ResetDispatched(this DownloadManagerState state)
        => new(Queued: [.. state.Dispatched, .. state.Queued], Dispatched: new HashSet<Guid>());

    public static bool Contains(this DownloadManagerState state, Guid downloadId)
        => state.Queued.Contains(downloadId) || state.Dispatched.Contains(downloadId);

    public static QueueResult PaginateQueue(QueueItem[] items, QueryQueue query, int totalSlots)
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

        return new QueueResult(paged.ToArray(), totalSlots, totalItems);
    }
}
