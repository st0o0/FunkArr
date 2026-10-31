using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;

namespace FunkArr.ArrApi.Newznab;

public static class NewznabFeedCache
{
    public const string PolicyName = "NewznabFeed";

    internal static readonly TimeSpan Duration = TimeSpan.FromSeconds(60);

    private static readonly string[] _feedFunctions = ["tvsearch", "movie", "search"];
    private static readonly string[] _criteria = ["q", "tvdbid", "imdbid", "tmdbid"];

    public static void AddPolicy(OutputCacheOptions options) =>
        options.AddPolicy(PolicyName, builder => builder
            .With(context => IsFeedRequest(context.HttpContext.Request.Query))
            .SetVaryByQuery("t", "cat", "offset", "limit", "apikey")
            .SetVaryByHost(true)
            .VaryByValue(http => new KeyValuePair<string, string>("scheme", http.Request.Scheme))
            .Expire(Duration));

    internal static bool IsFeedRequest(IQueryCollection query) =>
        _feedFunctions.Contains(query["t"].ToString())
        && _criteria.All(key => string.IsNullOrWhiteSpace(query[key]));
}
