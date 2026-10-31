using FunkArr.ArrApi.Newznab;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NewznabFeedCacheTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private int _calls;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOutputCache(NewznabFeedCache.AddPolicy);

        _app = builder.Build();
        _app.UseOutputCache();
        _app.MapGet("/index/api", () => $"call {Interlocked.Increment(ref _calls)}")
            .CacheOutput(NewznabFeedCache.PolicyName);

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Repeated_feed_page_is_served_from_cache()
    {
        var first = await Get("/index/api?t=tvsearch&offset=0&limit=100&apikey=key");
        var second = await Get("/index/api?t=tvsearch&offset=0&limit=100&apikey=key");

        Assert.Multiple(
            () => Assert.Equal("call 1", first),
            () => Assert.Equal("call 1", second),
            () => Assert.Equal(1, _calls));
    }

    [Theory]
    [InlineData("/index/api?t=tvsearch&offset=100&limit=100&apikey=key")]
    [InlineData("/index/api?t=tvsearch&offset=0&limit=100&apikey=other-key")]
    [InlineData("/index/api?t=movie&offset=0&limit=100&apikey=key")]
    [InlineData("/index/api?t=search&cat=5000&offset=0&limit=100&apikey=key")]
    public async Task Different_page_category_or_api_key_is_a_cache_miss(string url)
    {
        await Get("/index/api?t=tvsearch&offset=0&limit=100&apikey=key");

        var response = await Get(url);

        Assert.Multiple(
            () => Assert.Equal("call 2", response),
            () => Assert.Equal(2, _calls));
    }

    [Theory]
    [InlineData("/index/api?t=tvsearch&q=Tatort&apikey=key")]
    [InlineData("/index/api?t=tvsearch&tvdbid=83214&apikey=key")]
    [InlineData("/index/api?t=movie&imdbid=tt0082096&apikey=key")]
    [InlineData("/index/api?t=movie&tmdbid=387&apikey=key")]
    [InlineData("/index/api?t=get&id=abc&apikey=key")]
    [InlineData("/index/api?t=caps&apikey=key")]
    public async Task Searches_and_other_functions_are_not_cached(string url)
    {
        await Get(url);
        await Get(url);

        Assert.Equal(2, _calls);
    }

    [Theory]
    [InlineData("tvsearch", true)]
    [InlineData("movie", true)]
    [InlineData("search", true)]
    [InlineData("get", false)]
    [InlineData("caps", false)]
    [InlineData("", false)]
    public void Feed_request_is_a_criteria_free_search_function(string function, bool expected)
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["t"] = function });

        Assert.Equal(expected, NewznabFeedCache.IsFeedRequest(query));
    }

    private async Task<string> Get(string url) => await _client.GetStringAsync(url);
}
