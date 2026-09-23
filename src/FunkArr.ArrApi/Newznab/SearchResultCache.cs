using System.Collections.Concurrent;
using FunkArr.Messages.Search;

namespace FunkArr.ArrApi.Newznab;

public sealed class SearchResultCache(TimeSpan ttl, TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<SearchResultItem[]>>> _pending = new();

    public bool TryGet(string key, out SearchResultItem[]? items)
    {
        if (_cache.TryGetValue(key, out var entry) && timeProvider.GetUtcNow() < entry.Expiry)
        {
            items = entry.Items;
            return true;
        }

        items = null;
        return false;
    }

    public void Set(string key, SearchResultItem[] items)
    {
        _cache[key] = new CacheEntry(items, timeProvider.GetUtcNow() + ttl);
    }

    public async Task<SearchResultItem[]> GetOrAddAsync(
        string key, Func<Task<SearchResultItem[]>> factory)
    {
        if (TryGet(key, out var cached))
        {
            return cached!;
        }

        var lazy = _pending.GetOrAdd(key, _ => new Lazy<Task<SearchResultItem[]>>(factory));
        try
        {
            var result = await lazy.Value;
            Set(key, result);
            return result;
        }
        finally
        {
            _pending.TryRemove(key, out _);
        }
    }

    public static string BuildKey(SearchCommand cmd) => cmd.Params switch
    {
        SearchCommand.TvParams tv =>
            $"tv:{Normalize(cmd.Query)}:{tv.TvdbId}:{tv.ImdbId}",
        SearchCommand.MovieParams movie =>
            $"movie:{Normalize(cmd.Query)}:{movie.ImdbId}:{movie.TmdbId}",
        _ =>
            $"general:{Normalize(cmd.Query)}:{cmd.Cat}",
    };

    private static string Normalize(string? value) =>
        value?.Trim().ToLowerInvariant() ?? "";

    private sealed record CacheEntry(SearchResultItem[] Items, DateTimeOffset Expiry);
}
