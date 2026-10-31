using FunkArr.ArrApi.Newznab;
using FunkArr.Messages;
using FunkArr.Messages.Feed;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class FeedResultCacheTests
{
    private static readonly FeedPageKey _shows = new(MediaType.Show, 0, 100);
    private static readonly FeedResult _result = new([], 3);

    [Fact]
    public void Stored_page_is_returned_for_the_same_key_only()
    {
        var cache = new FeedResultCache();

        cache.Store(_shows, _result, cache.Generation);

        Assert.True(cache.TryGet(_shows, out var hit));
        Assert.Multiple(
            () => Assert.Same(_result, hit),
            () => Assert.False(cache.TryGet(_shows with { Offset = 100 }, out _)),
            () => Assert.False(cache.TryGet(_shows with { MediaType = MediaType.Movie }, out _)));
    }

    [Fact]
    public void Clear_removes_all_pages()
    {
        var cache = new FeedResultCache();
        cache.Store(_shows, _result, cache.Generation);

        cache.Clear();

        Assert.False(cache.TryGet(_shows, out _));
    }

    [Fact]
    public void Result_fetched_before_a_clear_is_not_stored()
    {
        var cache = new FeedResultCache();
        var generation = cache.Generation;

        cache.Clear();
        cache.Store(_shows, _result, generation);

        Assert.Multiple(
            () => Assert.False(cache.TryGet(_shows, out _)),
            () => Assert.Equal(generation + 1, cache.Generation));
    }
}
