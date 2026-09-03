using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed record DownloadManagerState(
    IReadOnlyList<Guid> Queued,
    IReadOnlySet<Guid> Dispatched)
{
    public static readonly DownloadManagerState Empty = new([], new HashSet<Guid>());
}

public static class DownloadManagerStateExtensions
{
    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadEnqueued evt) =>
        state with { Queued = [.. state.Queued, evt.DownloadId] };

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadDispatched evt) =>
        state with
        {
            Queued = state.Queued.Where(id => id != evt.DownloadId).ToArray(),
            Dispatched = state.Dispatched.Append(evt.DownloadId).ToHashSet(),
        };

    public static DownloadManagerState Apply(this DownloadManagerState state, DownloadDequeued evt) =>
        state with
        {
            Queued = state.Queued.Where(id => id != evt.DownloadId).ToArray(),
            Dispatched = state.Dispatched.Where(id => id != evt.DownloadId).ToHashSet(),
        };

    public static DownloadManagerState ResetDispatched(this DownloadManagerState state) =>
        state with
        {
            Queued = [.. state.Dispatched, .. state.Queued],
            Dispatched = new HashSet<Guid>(),
        };

    public static bool Contains(this DownloadManagerState state, Guid downloadId) =>
        state.Queued.Contains(downloadId) || state.Dispatched.Contains(downloadId);

    public static QueueResult PaginateQueue(QueueItem[] items, QueryQueue query, int totalSlots)
    {
        IEnumerable<QueueItem> filtered = items;
        if (query.Category is not null)
        {
            filtered = filtered.Where(i => string.Equals(i.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        }

        var materialized = filtered.ToArray();
        var totalItems = materialized.Length;

        IEnumerable<QueueItem> paged = materialized.Skip(query.Start);
        if (query.Limit > 0)
        {
            paged = paged.Take(query.Limit);
        }

        return new QueueResult(paged.ToArray(), totalSlots, totalItems);
    }
}
