using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed.Store;

public sealed class FeedStoreManagerState
{
    private static readonly IComparer<FeedEntry> _newestFirst = Comparer<FeedEntry>.Create((a, b) =>
    {
        var byFirstSeen = b.FirstSeenAt.CompareTo(a.FirstSeenAt);
        return byFirstSeen != 0 ? byFirstSeen : StringComparer.Ordinal.Compare(a.StableId, b.StableId);
    });

    private Dictionary<string, FeedEntry> Entries { get; } = new(StringComparer.Ordinal);
    private SortedSet<FeedEntry> Newest { get; } = new(_newestFirst);
    private Dictionary<MediaType, SortedSet<FeedEntry>> NewestByType { get; } = [];
    public int Count => Entries.Count;

    public static FeedStoreManagerState FromPersistence(PersistedFeedStoreState persisted)
    {
        var state = new FeedStoreManagerState();
        state.Apply(new FeedEntriesAdded(persisted.Entries));
        return state;
    }

    public FeedEntry[] NewEntries(IEnumerable<FeedEntry> candidates) =>
    [
        .. candidates
            .Where(c => !Entries.ContainsKey(c.StableId))
            .GroupBy(c => c.StableId, StringComparer.Ordinal)
            .Select(g => g.OrderBy(c => c.FirstSeenAt).First())
    ];

    public void Apply(FeedEntriesAdded evt)
    {
        foreach (var persisted in evt.Entries)
        {
            if (Entries.ContainsKey(persisted.StableId))
            {
                continue;
            }

            var entry = persisted.ToDomain();
            Entries[entry.StableId] = entry;
            Newest.Add(entry);

            if (!NewestByType.TryGetValue(entry.MediaType, out var byType))
            {
                byType = new SortedSet<FeedEntry>(_newestFirst);
                NewestByType[entry.MediaType] = byType;
            }

            byType.Add(entry);
        }
    }

    public void Apply(FeedEntriesExpired evt)
    {
        foreach (var stableId in evt.StableIds)
        {
            if (!Entries.Remove(stableId, out var entry))
            {
                continue;
            }

            Newest.Remove(entry);

            if (NewestByType.TryGetValue(entry.MediaType, out var byType))
            {
                byType.Remove(entry);
            }
        }
    }

    public string[] ExpiredIds(DateTimeOffset now, TimeSpan retention)
    {
        var threshold = now - retention;
        return [.. Newest.Reverse().TakeWhile(e => e.FirstSeenAt < threshold).Select(e => e.StableId)];
    }

    public FeedResult Query(QueryFeed query)
    {
        IEnumerable<FeedEntry> source = query.MediaType is { } mediaType
            ? NewestByType.TryGetValue(mediaType, out var byType) ? byType : []
            : Newest;

        if (query.Channel is { } channel)
        {
            source = source.Where(e => e.Channel.Contains(channel, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Quality is { } quality)
        {
            source = source.Where(e => e.Quality == quality);
        }

        if (query.Search is { Length: > 0 } search)
        {
            source = source.Where(e =>
                e.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Topic.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = source.ToArray();
        var page = filtered
            .Skip(Math.Max(0, query.Offset))
            .Take(Math.Max(0, query.Limit))
            .ToArray();

        return new FeedResult(page, Newest.Count, filtered.Length);
    }

    public PersistedFeedStoreState GetPersistenceState() =>
        new([.. Newest.Select(e => e.ToPersistence())]);
}
