using FunkArr.Messages.Mediathek;

namespace FunkArr.Feed;

internal sealed record CrawlWindow(long LowerBound, long UpperBound, bool Truncated = false);

internal sealed record PageEvaluation(MediathekItem[] Collected, bool ReachedEnd, int InProgress);

internal static class FeedCrawlPlanner
{
    internal static readonly TimeSpan MaxLead = TimeSpan.FromDays(90);

    internal static bool HasUpdate(long cursor, long listTimestamp) =>
        listTimestamp > 0 && listTimestamp > cursor;

    internal static CrawlWindow Window(long cursor, long listTimestamp, TimeSpan initialLookback, TimeSpan maxCatchUp)
    {
        if (cursor <= 0)
        {
            return new CrawlWindow(Math.Max(0, listTimestamp - (long)initialLookback.TotalSeconds), listTimestamp);
        }

        var earliest = listTimestamp - (long)maxCatchUp.TotalSeconds;
        return cursor < earliest
            ? new CrawlWindow(earliest, listTimestamp, Truncated: true)
            : new CrawlWindow(cursor, listTimestamp);
    }

    internal static PageEvaluation EvaluatePage(IReadOnlyList<MediathekItem> page, CrawlWindow window, int pageSize)
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

    internal static Func<MediathekItem, IEnumerable<MediathekItem>> DistinctByStableId()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return item => PrimaryUrl(item) is { } url && seen.Add(FeedGate.StableIdFor(item.Channel, url)) ? [item] : [];
    }
}
