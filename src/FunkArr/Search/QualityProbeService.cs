using System.Collections.Concurrent;
using System.Net.Http.Headers;
using FunkArr.Configuration;
using FunkArr.Shared.Models;
using Microsoft.Extensions.Options;

namespace FunkArr.Search;

public sealed class QualityProbeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QualityProbeService> _logger;
    private readonly FunkArrOptions _options;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, Task<QualityInfo>> _inflightProbes = new();

    private static readonly TimeSpan HeadTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RangeTimeout = TimeSpan.FromSeconds(10);
    private const int ContainerProbeBytes = 32768;

    public QualityProbeService(
        IHttpClientFactory httpClientFactory,
        ILogger<QualityProbeService> logger,
        IOptions<FunkArrOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<QualityInfo> ProbeAsync(string url, QualityTier fallbackTier, int durationSeconds)
    {
        if (!_options.QualityProbing)
        {
            return EstimatedFromTier(fallbackTier, durationSeconds);
        }

        if (TryGetCached(url, out var cached))
        {
            return cached!;
        }

        return await _inflightProbes.GetOrAdd(url, async u =>
        {
            try
            {
                var result = await ProbeInternalAsync(u, fallbackTier, durationSeconds);
                StoreInCache(u, result);
                return result;
            }
            finally
            {
                _inflightProbes.TryRemove(u, out _);
            }
        });
    }

    public async Task<IReadOnlyList<SearchResult>> ExpandWithProbingAsync(
        MediathekResultItem item, int probeLimit, int currentCount)
    {
        var results = new List<SearchResult>();
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp);

        var urls = new (string Url, QualityTier FallbackTier)[]
        {
            (item.Url_Video_HD, QualityTier.HD720),
            (item.Url_Video, QualityTier.HD720),
            (item.Url_Video_Low, QualityTier.SD),
        };

        foreach (var (url, fallbackTier) in urls)
        {
            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            var shouldProbe = currentCount + results.Count < probeLimit;
            var qualityInfo = shouldProbe
                ? await ProbeAsync(url, fallbackTier, item.Duration)
                : EstimatedFromTier(fallbackTier, item.Duration);

            results.Add(new SearchResult
            {
                Title = item.Title,
                Topic = item.Topic,
                Channel = item.Channel,
                Url = url,
                UrlSubtitle = string.IsNullOrEmpty(item.Url_Subtitle) ? null : item.Url_Subtitle,
                DurationSeconds = item.Duration,
                SizeBytes = qualityInfo.FileSize,
                Timestamp = timestamp,
                Description = string.IsNullOrEmpty(item.Description) ? null : item.Description,
                Quality = qualityInfo.QualityTier,
                QualityInfo = qualityInfo,
            });
        }

        return results;
    }

    private async Task<QualityInfo> ProbeInternalAsync(
        string url, QualityTier fallbackTier, int durationSeconds)
    {
        var patternResult = UrlPatternAnalyzer.Analyze(url);

        HeadProbeResult? headResult = null;
        try
        {
            headResult = await ProbeHeadAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "HEAD probe failed for {Url}", url);
        }

        var fileSize = headResult?.ContentLength
            ?? EstimateSizeFromBitrate(patternResult?.BitrateKbps, durationSeconds)
            ?? EstimateSize(durationSeconds, fallbackTier);

        var container = headResult?.Container ?? "mp4";

        if (patternResult?.Resolution is not null && patternResult.Codec is not null)
        {
            return new QualityInfo
            {
                Resolution = patternResult.Resolution.Value,
                Codec = patternResult.Codec,
                BitrateKbps = patternResult.BitrateKbps,
                FileSize = fileSize,
                Container = container,
                ProbeSource = ProbeSource.UrlPattern,
            };
        }

        if (!UrlPatternAnalyzer.IsNonProbeable(url) && container is "mp4")
        {
            try
            {
                var containerResult = await ProbeContainerAsync(url);
                if (containerResult is not null)
                {
                    return new QualityInfo
                    {
                        Resolution = containerResult.Resolution,
                        Codec = containerResult.Codec,
                        BitrateKbps = patternResult?.BitrateKbps,
                        FileSize = fileSize,
                        Container = container,
                        ProbeSource = ProbeSource.ContainerHeader,
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Container probe failed for {Url}", url);
            }
        }

        if (patternResult?.Resolution is not null)
        {
            return new QualityInfo
            {
                Resolution = patternResult.Resolution.Value,
                Codec = patternResult.Codec ?? "h264",
                BitrateKbps = patternResult.BitrateKbps,
                FileSize = fileSize,
                Container = container,
                ProbeSource = ProbeSource.UrlPattern,
            };
        }

        if (headResult is not null)
        {
            return new QualityInfo
            {
                Resolution = TierToResolution(fallbackTier),
                FileSize = fileSize,
                Container = container,
                ProbeSource = ProbeSource.Head,
            };
        }

        return EstimatedFromTier(fallbackTier, durationSeconds);
    }

    internal async Task<HeadProbeResult?> ProbeHeadAsync(string url)
    {
        using var client = _httpClientFactory.CreateClient("QualityProbe");
        using var cts = new CancellationTokenSource(HeadTimeout);

        var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var contentLength = response.Content.Headers.ContentLength;
        var contentType = response.Content.Headers.ContentType?.MediaType;

        var container = contentType switch
        {
            "video/mp4" => "mp4",
            "video/webm" => "webm",
            "video/x-matroska" => "mkv",
            "application/x-mpegURL" => "m3u8",
            _ => null,
        };

        return new HeadProbeResult
        {
            ContentLength = contentLength,
            Container = container,
        };
    }

    internal async Task<Mp4ProbeResult?> ProbeContainerAsync(string url)
    {
        using var client = _httpClientFactory.CreateClient("QualityProbe");
        using var cts = new CancellationTokenSource(RangeTimeout);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(0, ContainerProbeBytes - 1);

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

        if (response.StatusCode != System.Net.HttpStatusCode.PartialContent)
        {
            return null;
        }

        var data = await response.Content.ReadAsByteArrayAsync(cts.Token);
        return Mp4AtomParser.Parse(data);
    }

    private bool TryGetCached(string url, out QualityInfo? info)
    {
        if (_cache.TryGetValue(url, out var entry))
        {
            if (DateTimeOffset.UtcNow - entry.CreatedAt < TimeSpan.FromMinutes(_options.QualityCacheTtlMinutes))
            {
                info = entry.Info;
                return true;
            }

            _cache.TryRemove(url, out _);
        }

        info = null;
        return false;
    }

    private void StoreInCache(string url, QualityInfo info)
    {
        if (_cache.Count >= _options.QualityCacheCapacity)
        {
            EvictOldest();
        }

        _cache[url] = new CacheEntry(info, DateTimeOffset.UtcNow);
    }

    private void EvictOldest()
    {
        var oldest = _cache
            .OrderBy(kvp => kvp.Value.CreatedAt)
            .Take(Math.Max(1, _cache.Count / 10))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in oldest)
            _cache.TryRemove(key, out _);
    }

    internal static QualityInfo EstimatedFromTier(QualityTier tier, int durationSeconds) =>
        QualityInfo.Estimated(tier, EstimateSize(durationSeconds, tier));

    internal static long EstimateSize(int durationSeconds, QualityTier quality)
    {
        var bitrateKbps = quality switch
        {
            QualityTier.HD1080 => 4000,
            QualityTier.HD720 => 2000,
            _ => 800,
        };
        return (long)durationSeconds * bitrateKbps * 1000 / 8;
    }

    private static long? EstimateSizeFromBitrate(int? bitrateKbps, int durationSeconds) =>
        bitrateKbps is > 0 ? (long)durationSeconds * bitrateKbps.Value * 1000 / 8 : null;

    private static Resolution TierToResolution(QualityTier tier) => tier switch
    {
        QualityTier.HD1080 => new Resolution(1920, 1080),
        QualityTier.HD720 => new Resolution(1280, 720),
        _ => new Resolution(640, 480),
    };

    private sealed record CacheEntry(QualityInfo Info, DateTimeOffset CreatedAt);
}

internal sealed record HeadProbeResult
{
    public long? ContentLength { get; init; }
    public string? Container { get; init; }
}
