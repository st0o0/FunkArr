using FunkArr.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FunkArr.Search.Tests;

public sealed class DistributedCacheExtensionsTests
{
    private static MemoryDistributedCache CreateCache() => new(Options.Create(new MemoryDistributedCacheOptions()));

    private sealed record TestData(string Name, int Value);

    [Fact]
    public async Task GetAsync_returns_default_for_missing_key()
    {
        var cache = CreateCache();

        var result = await cache.GetAsync<TestData>("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_and_GetAsync_round_trip()
    {
        var cache = CreateCache();
        var data = new TestData("test", 42);

        await cache.SetAsync("key", data, TimeSpan.FromMinutes(5));
        var result = await cache.GetAsync<TestData>("key");

        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task SetAsync_overwrites_existing_entry()
    {
        var cache = CreateCache();

        await cache.SetAsync("key", new TestData("first", 1), TimeSpan.FromMinutes(5));
        await cache.SetAsync("key", new TestData("second", 2), TimeSpan.FromMinutes(5));
        var result = await cache.GetAsync<TestData>("key");

        Assert.NotNull(result);
        Assert.Equal("second", result.Name);
    }
}
