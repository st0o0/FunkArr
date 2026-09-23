using FunkArr.ArrApi.Newznab;
using FunkArr.Messages;
using FunkArr.Messages.Search;
using Microsoft.Extensions.Time.Testing;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class SearchResultCacheTests
{
    private static SearchResultItem MakeItem(string title) =>
        new(title, "ARD", "Topic", "url", 3600, 100, 720, null, 0.9);

    [Fact]
    public void TryGet_unknown_key_returns_false()
    {
        var cache = new SearchResultCache(TimeSpan.FromSeconds(60), new FakeTimeProvider());

        Assert.False(cache.TryGet("unknown", out _));
    }

    [Fact]
    public void Set_then_TryGet_returns_cached_items()
    {
        var cache = new SearchResultCache(TimeSpan.FromSeconds(60), new FakeTimeProvider());
        var items = new[] { MakeItem("A"), MakeItem("B") };

        cache.Set("key1", items);

        Assert.True(cache.TryGet("key1", out var result));
        Assert.Equal(2, result!.Length);
        Assert.Equal("A", result[0].Title);
    }

    [Fact]
    public void TryGet_returns_false_after_ttl_expires()
    {
        var time = new FakeTimeProvider();
        var cache = new SearchResultCache(TimeSpan.FromSeconds(60), time);
        cache.Set("key1", [MakeItem("A")]);

        time.Advance(TimeSpan.FromSeconds(61));

        Assert.False(cache.TryGet("key1", out _));
    }

    [Fact]
    public void TryGet_returns_true_before_ttl_expires()
    {
        var time = new FakeTimeProvider();
        var cache = new SearchResultCache(TimeSpan.FromSeconds(60), time);
        cache.Set("key1", [MakeItem("A")]);

        time.Advance(TimeSpan.FromSeconds(59));

        Assert.True(cache.TryGet("key1", out _));
    }

    [Fact]
    public void BuildKey_tv_search_excludes_offset_and_limit()
    {
        var cmd1 = new SearchCommand(SearchSource.Sonarr, null, null, 100, 0,
            new SearchCommand.TvParams(2026, 18, 83214, null));
        var cmd2 = new SearchCommand(SearchSource.Sonarr, null, null, 100, 200,
            new SearchCommand.TvParams(2026, 18, 83214, null));

        Assert.Equal(SearchResultCache.BuildKey(cmd1), SearchResultCache.BuildKey(cmd2));
    }

    [Fact]
    public void BuildKey_different_tvdbId_produces_different_key()
    {
        var cmd1 = new SearchCommand(SearchSource.Sonarr, null, null, 100, 0,
            new SearchCommand.TvParams(2026, 18, 83214, null));
        var cmd2 = new SearchCommand(SearchSource.Sonarr, null, null, 100, 0,
            new SearchCommand.TvParams(2026, 18, 99999, null));

        Assert.NotEqual(SearchResultCache.BuildKey(cmd1), SearchResultCache.BuildKey(cmd2));
    }

    [Fact]
    public void BuildKey_different_season_same_series_shares_key()
    {
        var cmd1 = new SearchCommand(SearchSource.Sonarr, null, null, 100, 0,
            new SearchCommand.TvParams(2026, 18, 83214, null));
        var cmd2 = new SearchCommand(SearchSource.Sonarr, null, null, 100, 0,
            new SearchCommand.TvParams(2025, 18, 83214, null));

        Assert.Equal(SearchResultCache.BuildKey(cmd1), SearchResultCache.BuildKey(cmd2));
    }

    [Fact]
    public void BuildKey_different_episode_same_series_shares_key()
    {
        var cmd1 = new SearchCommand(SearchSource.Sonarr, null, null, 100, 0,
            new SearchCommand.TvParams(2026, 17, 83214, null));
        var cmd2 = new SearchCommand(SearchSource.Sonarr, null, null, 100, 0,
            new SearchCommand.TvParams(2026, 18, 83214, null));

        Assert.Equal(SearchResultCache.BuildKey(cmd1), SearchResultCache.BuildKey(cmd2));
    }

    [Fact]
    public void BuildKey_tv_vs_movie_produces_different_key()
    {
        var tvCmd = new SearchCommand(SearchSource.Sonarr, "test", null, 100, 0,
            new SearchCommand.TvParams(null, null, null, "tt123"));
        var movieCmd = new SearchCommand(SearchSource.Radarr, "test", null, 100, 0,
            new SearchCommand.MovieParams("tt123", null));

        Assert.NotEqual(SearchResultCache.BuildKey(tvCmd), SearchResultCache.BuildKey(movieCmd));
    }

    [Fact]
    public void BuildKey_source_excluded_from_key()
    {
        var cmd1 = new SearchCommand(SearchSource.Sonarr, null, null, 100, 0,
            new SearchCommand.TvParams(2026, 18, 83214, null));
        var cmd2 = new SearchCommand(SearchSource.Prowlarr, null, null, 100, 0,
            new SearchCommand.TvParams(2026, 18, 83214, null));

        Assert.Equal(SearchResultCache.BuildKey(cmd1), SearchResultCache.BuildKey(cmd2));
    }

    [Fact]
    public void BuildKey_general_search_includes_cat()
    {
        var cmd1 = new SearchCommand(SearchSource.Prowlarr, "tatort", 5030, 100, 0, null);
        var cmd2 = new SearchCommand(SearchSource.Prowlarr, "tatort", 2030, 100, 0, null);

        Assert.NotEqual(SearchResultCache.BuildKey(cmd1), SearchResultCache.BuildKey(cmd2));
    }

    [Fact]
    public async Task GetOrAddAsync_concurrent_same_key_single_factory_call()
    {
        var cache = new SearchResultCache(TimeSpan.FromSeconds(60), new FakeTimeProvider());
        var callCount = 0;
        var tcs = new TaskCompletionSource<SearchResultItem[]>();

        var task1 = cache.GetOrAddAsync("key", () =>
        {
            Interlocked.Increment(ref callCount);
            return tcs.Task;
        });

        var task2 = cache.GetOrAddAsync("key", () =>
        {
            Interlocked.Increment(ref callCount);
            return tcs.Task;
        });

        tcs.SetResult([MakeItem("A")]);

        var result1 = await task1;
        var result2 = await task2;

        Assert.Equal(1, callCount);
        Assert.Same(result1, result2);
    }

    [Fact]
    public async Task GetOrAddAsync_returns_cached_without_factory()
    {
        var cache = new SearchResultCache(TimeSpan.FromSeconds(60), new FakeTimeProvider());
        var items = new[] { MakeItem("Cached") };
        cache.Set("key", items);

        var factoryCalled = false;
        var result = await cache.GetOrAddAsync("key", () =>
        {
            factoryCalled = true;
            return Task.FromResult(new[] { MakeItem("Fresh") });
        });

        Assert.False(factoryCalled);
        Assert.Equal("Cached", result[0].Title);
    }

    [Fact]
    public async Task GetOrAddAsync_factory_failure_does_not_cache()
    {
        var cache = new SearchResultCache(TimeSpan.FromSeconds(60), new FakeTimeProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrAddAsync("key", () => Task.FromException<SearchResultItem[]>(new InvalidOperationException())));

        Assert.False(cache.TryGet("key", out _));
    }
}
