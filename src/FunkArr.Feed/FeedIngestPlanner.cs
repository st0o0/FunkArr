using FunkArr.Messages.Mediathek;

namespace FunkArr.Feed;

internal sealed record IngestWindow(long LowerBound, long UpperBound);

internal sealed record PageEvaluation(MediathekItem[] Collected, bool ReachedEnd, int InProgress);

internal static class FeedIngestPlanner
{
    internal static readonly TimeSpan MaxLead = TimeSpan.FromDays(90);

    internal static bool HasUpdate(long cursor, long listTimestamp) =>
        listTimestamp > 0 && listTimestamp > cursor;

    internal static IngestWindow Window(long cursor, long listTimestamp, TimeSpan initialLookback)
    {
        var lowerBound = cursor > 0
            ? cursor
            : Math.Max(0, listTimestamp - (long)initialLookback.TotalSeconds);
        return new IngestWindow(lowerBound, listTimestamp);
    }

    internal static PageEvaluation EvaluatePage(IReadOnlyList<MediathekItem> page, IngestWindow window, int pageSize)
    {
        var collected = new List<MediathekItem>(page.Count);
        var inProgress = 0;
        var reachedOlder = false;

        foreach (var item in page)
        {
            var timestamp = item.FilmlisteTimestamp;

            if (timestamp > window.UpperBound)
            {
                inProgress++;
                continue;
            }

            if (timestamp <= window.LowerBound)
            {
                reachedOlder = true;
                break;
            }

            collected.Add(item);
        }

        return new PageEvaluation([.. collected], reachedOlder || page.Count < pageSize, inProgress);
    }

    internal static MediathekItem[] WithinLead(IEnumerable<MediathekItem> items, DateTimeOffset now)
    {
        var limit = (now + MaxLead).ToUnixTimeSeconds();
        return [.. items.Where(i => i.Timestamp <= limit)];
    }

    internal static string? PrimaryUrl(MediathekItem item) =>
        item.UrlVideoHd ?? item.UrlVideo ?? item.UrlVideoLow;

    internal static MediathekItem[] Distinct(IEnumerable<MediathekItem> items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<MediathekItem>();

        foreach (var item in items)
        {
            var url = PrimaryUrl(item);
            if (url is null)
            {
                continue;
            }

            if (seen.Add(FeedGate.StableIdFor(item.Channel, url)))
            {
                result.Add(item);
            }
        }

        return [.. result];
    }
}
