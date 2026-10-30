using System.Collections.Immutable;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed;

public sealed record FeedManagerState(
    ImmutableDictionary<string, FeedEntry> Entries,
    ImmutableSortedSet<FeedEntry> Newest,
    ImmutableDictionary<MediaType, ImmutableSortedSet<FeedEntry>> NewestByType)
{
    public static readonly IComparer<FeedEntry> NewestFirst = Comparer<FeedEntry>.Create((a, b) =>
    {
        var byFirstSeen = b.FirstSeenAt.CompareTo(a.FirstSeenAt);
        return byFirstSeen != 0 ? byFirstSeen : StringComparer.Ordinal.Compare(a.StableId, b.StableId);
    });

    public static readonly FeedManagerState Empty = new(
        ImmutableDictionary.Create<string, FeedEntry>(StringComparer.Ordinal),
        ImmutableSortedSet.Create(NewestFirst),
        ImmutableDictionary<MediaType, ImmutableSortedSet<FeedEntry>>.Empty);

    public static FeedManagerState FromPersistence(PersistedFeedState persisted) =>
        Empty.Apply(new FeedEntriesAdded(persisted.Entries));
}

public static class FeedManagerStateExtensions
{
    public static FeedEntry[] NewEntries(this FeedManagerState state, IEnumerable<FeedEntry> candidates) =>
    [
        .. candidates
            .Where(c => !state.Entries.ContainsKey(c.StableId))
            .GroupBy(c => c.StableId, StringComparer.Ordinal)
            .Select(g => g.OrderBy(c => c.FirstSeenAt).First())
    ];

    public static FeedManagerState Apply(this FeedManagerState state, FeedEntriesAdded evt)
    {
        var entries = state.Entries.ToBuilder();
        var newest = state.Newest.ToBuilder();
        var byType = new Dictionary<MediaType, ImmutableSortedSet<FeedEntry>.Builder>();

        foreach (var persisted in evt.Entries)
        {
            if (entries.ContainsKey(persisted.StableId))
            {
                continue;
            }

            var entry = persisted.ToDomain();
            entries[entry.StableId] = entry;
            newest.Add(entry);
            TypeBuilder(state, byType, entry.MediaType).Add(entry);
        }

        return new FeedManagerState(entries.ToImmutable(), newest.ToImmutable(), Merge(state, byType));
    }

    public static FeedManagerState Apply(this FeedManagerState state, FeedEntriesExpired evt)
    {
        var entries = state.Entries.ToBuilder();
        var newest = state.Newest.ToBuilder();
        var byType = new Dictionary<MediaType, ImmutableSortedSet<FeedEntry>.Builder>();

        foreach (var stableId in evt.StableIds)
        {
            if (!entries.TryGetValue(stableId, out var entry))
            {
                continue;
            }

            entries.Remove(stableId);
            newest.Remove(entry);
            TypeBuilder(state, byType, entry.MediaType).Remove(entry);
        }

        return new FeedManagerState(entries.ToImmutable(), newest.ToImmutable(), Merge(state, byType));
    }

    public static string[] ExpiredIds(this FeedManagerState state, DateTimeOffset now, TimeSpan retention)
    {
        var threshold = now - retention;
        return [.. state.Newest.Reverse().TakeWhile(e => e.FirstSeenAt < threshold).Select(e => e.StableId)];
    }

    public static FeedResult Query(this FeedManagerState state, QueryFeed query)
    {
        var source = query.MediaType is { } mediaType
            ? state.ForType(mediaType)
            : state.Newest;

        var page = source
            .Skip(Math.Max(0, query.Offset))
            .Take(Math.Max(0, query.Limit))
            .ToArray();

        return new FeedResult(page, source.Count);
    }

    public static PersistedFeedState GetPersistenceState(this FeedManagerState state) =>
        new([.. state.Newest.Select(e => e.ToPersistence())]);

    private static ImmutableSortedSet<FeedEntry> ForType(this FeedManagerState state, MediaType mediaType) =>
        state.NewestByType.TryGetValue(mediaType, out var entries) ? entries : FeedManagerState.Empty.Newest;

    private static ImmutableSortedSet<FeedEntry>.Builder TypeBuilder(
        FeedManagerState state,
        Dictionary<MediaType, ImmutableSortedSet<FeedEntry>.Builder> builders,
        MediaType mediaType)
    {
        if (!builders.TryGetValue(mediaType, out var builder))
        {
            builder = state.ForType(mediaType).ToBuilder();
            builders[mediaType] = builder;
        }

        return builder;
    }

    private static ImmutableDictionary<MediaType, ImmutableSortedSet<FeedEntry>> Merge(
        FeedManagerState state,
        Dictionary<MediaType, ImmutableSortedSet<FeedEntry>.Builder> builders) =>
        builders.Aggregate(state.NewestByType, (byType, b) => byType.SetItem(b.Key, b.Value.ToImmutable()));
}
