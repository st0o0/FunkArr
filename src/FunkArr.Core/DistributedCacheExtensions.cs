using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace FunkArr.Core;

public static class DistributedCacheExtensions
{
    public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key, CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
    }

    public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration
        };
        return cache.SetAsync(key, bytes, options, cancellationToken);
    }
}
