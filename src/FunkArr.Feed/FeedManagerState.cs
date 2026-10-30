using System.Collections.Immutable;
using FunkArr.Messages.Feed;
using FunkArr.Persistence.Events.Feed;

namespace FunkArr.Feed;

public sealed record FeedManagerState(ImmutableDictionary<string, FeedEntry> Entries)
{
    public static readonly FeedManagerState Empty =
        new(ImmutableDictionary.Create<string, FeedEntry>(StringComparer.Ordinal));

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
        var builder = state.Entries.ToBuilder();
        foreach (var persisted in evt.Entries)
        {
            if (!builder.ContainsKey(persisted.StableId))
            {
                builder[persisted.StableId] = persisted.ToDomain();
            }
        }

        return new FeedManagerState(builder.ToImmutable());
    }

    public static FeedManagerState Apply(this FeedManagerState state, FeedEntriesExpired evt) =>
        new(state.Entries.RemoveRange(evt.StableIds));

    public static string[] ExpiredIds(this FeedManagerState state, DateTimeOffset now, TimeSpan retention)
    {
        var threshold = now - retention;
        return [.. state.Entries.Values.Where(e => e.FirstSeenAt < threshold).Select(e => e.StableId)];
    }

    public static FeedResult Query(this FeedManagerState state, QueryFeed query)
    {
        var matching = state.Entries.Values
            .Where(e => query.MediaType is null || e.MediaType == query.MediaType)
            .OrderByDescending(e => e.FirstSeenAt)
            .ThenBy(e => e.StableId, StringComparer.Ordinal)
            .ToArray();

        var page = matching
            .Skip(Math.Max(0, query.Offset))
            .Take(Math.Max(0, query.Limit))
            .ToArray();

        return new FeedResult(page, matching.Length);
    }

    public static PersistedFeedState GetPersistenceState(this FeedManagerState state) =>
        new([.. state.Entries.Values.Select(e => e.ToPersistence())]);
}
