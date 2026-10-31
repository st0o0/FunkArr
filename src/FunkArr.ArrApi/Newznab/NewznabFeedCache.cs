using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;

namespace FunkArr.ArrApi.Newznab;

public static class NewznabFeedCache
{
    public const string PolicyName = "NewznabFeed";
    public const string Tag = "feed";

    internal const string DoNotStoreKey = "FunkArr.NewznabFeed.DoNotStore";
    internal static readonly TimeSpan Duration = TimeSpan.FromMinutes(30);

    private static readonly string[] _feedFunctions = ["tvsearch", "movie", "search"];
    private static readonly string[] _criteria = ["q", "tvdbid", "imdbid", "tmdbid"];

    public static void AddPolicy(OutputCacheOptions options) =>
        options.AddPolicy(PolicyName, builder => builder
            .With(context => IsFeedRequest(context.HttpContext.Request.Query))
            .SetVaryByQuery("t", "cat", "offset", "limit", "apikey")
            .SetVaryByHost(true)
            .VaryByValue(http => new KeyValuePair<string, string>("scheme", http.Request.Scheme))
            .Tag(Tag)
            .Expire(Duration)
            .AddPolicy<SkipMarkedResponsesPolicy>());

    internal static bool IsFeedRequest(IQueryCollection query) =>
        _feedFunctions.Contains(query["t"].ToString())
        && _criteria.All(key => string.IsNullOrWhiteSpace(query[key]));

    internal static void DoNotStore(HttpContext context) => context.Items[DoNotStoreKey] = true;
}

internal sealed class SkipMarkedResponsesPolicy : IOutputCachePolicy
{
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        if (context.HttpContext.Items.ContainsKey(NewznabFeedCache.DoNotStoreKey))
        {
            context.AllowCacheStorage = false;
        }

        return ValueTask.CompletedTask;
    }
}
