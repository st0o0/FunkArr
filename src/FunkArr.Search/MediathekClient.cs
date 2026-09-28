using System.Net.Mime;
using System.Text.Json;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FunkArr.Search;

public sealed class MediathekClient(HttpClient httpClient, IDistributedCache cache, ILogger<MediathekClient> log)
{
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions _apiJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new EmptyStringToNullConverter() }
    };

    internal async Task<MediathekQueryResult> QueryAsync(string json, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"mediathek:{json}";

        var cached = await cache.GetAsync<MediathekQueryResult>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            log.LogDebug("MediathekViewWeb cache hit");
            Telemetry.MediathekCacheHits.Add(1);
            return cached;
        }

        Telemetry.MediathekCacheMisses.Add(1);

        using var content = new StringContent(json, System.Text.Encoding.UTF8, MediaTypeNames.Text.Plain);
        using var response = await httpClient.PostAsync("", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<MediathekApiResponse>(body, _apiJsonOptions);

        var items = (apiResponse?.Result?.Results ?? [])
            .Select(r => new MediathekItem(
                Channel: r.Channel ?? "",
                Topic: r.Topic ?? "",
                Title: r.Title ?? "",
                Description: r.Description,
                Timestamp: r.Timestamp,
                Duration: r.Duration,
                Size: r.Size ?? EstimateSize(r.Duration, r.UrlVideoHd, r.UrlVideo, r.UrlVideoLow),
                UrlVideoLow: r.UrlVideoLow,
                UrlVideo: r.UrlVideo,
                UrlVideoHd: r.UrlVideoHd,
                UrlSubtitle: r.UrlSubtitle,
                UrlWebsite: r.UrlWebsite))
            .ToArray();

        var total = apiResponse?.Result?.QueryInfo?.TotalResults ?? items.Length;
        var result = new MediathekQueryResult(items, total);

        await cache.SetAsync(cacheKey, result, _cacheTtl, cancellationToken);

        return result;
    }

    internal static long EstimateSize(int duration, string? urlHd, string? urlVideo, string? urlLow)
        => duration * (urlHd is not null ? 833_000L :
            urlVideo is not null ? 420_000L :
            urlLow is not null ? 100_000L : 0L);
}
