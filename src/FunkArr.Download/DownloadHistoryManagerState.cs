using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed record HistoryRecord(
    Guid DownloadId,
    string Title,
    string Category,
    long Size,
    DownloadStatus Status,
    string? RelativePath,
    string? FailMessage,
    int DownloadTimeSeconds,
    long CompletedAt);

public sealed record DownloadHistoryManagerState(IReadOnlyList<HistoryRecord> Records)
{
    public static readonly DownloadHistoryManagerState Empty = new([]);
}

public static class DownloadHistoryManagerStateExtensions
{
    public static DownloadHistoryManagerState Apply(this DownloadHistoryManagerState state, HistoryRecorded evt) =>
        state with
        {
            Records = [.. state.Records, new HistoryRecord(
                evt.DownloadId, evt.Title, evt.Category, evt.Size,
                (DownloadStatus)evt.Status, evt.RelativePath, evt.FailMessage,
                evt.DownloadTimeSeconds, evt.CompletedAt)],
        };

    public static DownloadHistoryManagerState Apply(this DownloadHistoryManagerState state, HistoryRemoved evt) =>
        state with
        {
            Records = state.Records.Where(r => r.DownloadId != evt.DownloadId).ToArray(),
        };

    public static bool Contains(this DownloadHistoryManagerState state, Guid downloadId) =>
        state.Records.Any(r => r.DownloadId == downloadId);

    public static HistoryResult ToHistoryResult(this DownloadHistoryManagerState state, QueryHistory query)
    {
        IEnumerable<HistoryRecord> filtered = state.Records;
        if (query.Category is not null)
        {
            filtered = filtered.Where(r => string.Equals(r.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        }

        var materialized = filtered.ToArray();
        var totalItems = materialized.Length;

        IEnumerable<HistoryRecord> paged = materialized.Skip(query.Start);
        if (query.Limit > 0)
        {
            paged = paged.Take(query.Limit);
        }

        return new HistoryResult(
            paged.Select(r => new HistoryItem(
                r.DownloadId, r.Title, r.Category, r.Size,
                r.DownloadTimeSeconds, r.RelativePath ?? "", r.Status,
                r.FailMessage ?? "", r.CompletedAt))
            .ToArray(),
            totalItems);
    }
}
