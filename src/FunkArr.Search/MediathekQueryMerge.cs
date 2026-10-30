using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

internal static class MediathekQueryMerge
{
    internal static QueryMediathekResponse Merge(IReadOnlyList<QueryMediathekResponse> responses)
    {
        if (responses.OfType<QueryMediathekQueueFull>().FirstOrDefault() is { } queueFull)
        {
            return queueFull;
        }

        if (responses.OfType<QueryMediathekError>().FirstOrDefault() is { } error)
        {
            return error;
        }

        var completed = responses.OfType<QueryMediathekCompleted>().ToArray();
        if (completed.Length == 0)
        {
            return new QueryMediathekError(new InvalidOperationException("No MediathekViewWeb response"));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var items = new List<MediathekItem>();
        foreach (var item in completed.SelectMany(c => c.Items))
        {
            if (seen.Add(KeyOf(item)))
            {
                items.Add(item);
            }
        }

        return new QueryMediathekCompleted(
            [.. items],
            completed.Max(c => c.Total),
            completed.Max(c => c.ListTimestamp));
    }

    private static string KeyOf(MediathekItem item) =>
        item.UrlVideoHd ?? item.UrlVideo ?? item.UrlVideoLow ?? $"{item.Channel}|{item.Title}|{item.Timestamp}";
}
