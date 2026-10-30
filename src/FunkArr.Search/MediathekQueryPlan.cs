using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

internal sealed class MediathekQueryPlan
{
    internal const int MaxPages = 3;
    internal static readonly TimeSpan MaxLead = TimeSpan.FromDays(90);

    private static readonly TimeSpan _airedTolerance = TimeSpan.FromHours(1);

    private readonly QueryMediathek _query;
    private readonly long _airedUntil;
    private readonly long _leadLimit;
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly List<MediathekItem> _items = [];
    private int _aired;
    private int _pages;
    private int _total;
    private long _listTimestamp;

    private MediathekQueryPlan(QueryMediathek query, DateTimeOffset now)
    {
        _query = query;
        _airedUntil = (now + _airedTolerance).ToUnixTimeSeconds();
        _leadLimit = (now + MaxLead).ToUnixTimeSeconds();
    }

    internal static MediathekQueryPlan For(QueryMediathek query, DateTimeOffset now) => new(query, now);

    internal bool KeepsAiredWindow =>
        (_query.SortBy ?? "timestamp") == "timestamp" && (_query.SortOrder ?? "desc") == "desc";

    internal string FirstPage() => PageJson(_query.Offset);

    internal string? Apply(MediathekQueryResult page)
    {
        _pages++;
        _total = Math.Max(_total, page.Total);
        _listTimestamp = Math.Max(_listTimestamp, page.ListTimestamp);

        foreach (var item in page.Items)
        {
            if (KeepsAiredWindow && _aired >= _query.Size)
            {
                break;
            }

            if (item.Timestamp > _leadLimit || !_seen.Add(KeyOf(item)))
            {
                continue;
            }

            _items.Add(item);
            if (item.Timestamp <= _airedUntil)
            {
                _aired++;
            }
        }

        var needsMore = KeepsAiredWindow
            && _aired < _query.Size
            && page.Items.Length >= _query.Size
            && _pages < MaxPages;

        return needsMore ? PageJson(_query.Offset + _pages * _query.Size) : null;
    }

    internal QueryMediathekCompleted ToCompleted() => new([.. _items], _total, _listTimestamp);

    private string PageJson(int offset) =>
        MediathekQueryBuilder.FromMessage(_query with { Offset = offset }).Build();

    private static string KeyOf(MediathekItem item) =>
        item.UrlVideoHd ?? item.UrlVideo ?? item.UrlVideoLow ?? $"{item.Channel}|{item.Title}|{item.Timestamp}";
}
