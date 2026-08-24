using FunkArr.Shared.Models;

namespace FunkArr.Search.Quality;

public static class QualityExpander
{
    public static IReadOnlyList<SearchResult> Expand(MediathekResultItem item)
    {
        var results = new List<SearchResult>();
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp);

        TryAdd(results, item, item.UrlVideoHd, QualityTier.HD1080, timestamp);
        TryAdd(results, item, item.UrlVideo, QualityTier.HD720, timestamp);
        TryAdd(results, item, item.UrlVideoLow, QualityTier.SD, timestamp);

        return results;
    }

    public static IReadOnlyList<SearchResult> ExpandMany(IEnumerable<MediathekResultItem> items)
    {
        var results = new List<SearchResult>();
        foreach (var item in items)
        {
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp);
            TryAdd(results, item, item.UrlVideoHd, QualityTier.HD1080, timestamp);
            TryAdd(results, item, item.UrlVideo, QualityTier.HD720, timestamp);
            TryAdd(results, item, item.UrlVideoLow, QualityTier.SD, timestamp);
        }

        return results;
    }

    private static void TryAdd(
        List<SearchResult> results, MediathekResultItem item,
        string url, QualityTier fallbackTier, DateTimeOffset timestamp)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        var pattern = UrlPatternAnalyzer.Analyze(url);
        var quality = pattern?.Resolution is not null
            ? ResolutionToTier(pattern.Resolution.Value)
            : fallbackTier;

        var codec = pattern?.Codec;
        var fileSize = EstimateSizeFromPattern(pattern, item.Duration)
            ?? EstimateSize(item.Duration, quality);

        results.Add(new SearchResult
        {
            Title = item.Title,
            Topic = item.Topic,
            Channel = item.Channel,
            Url = url,
            UrlSubtitle = string.IsNullOrEmpty(item.UrlSubtitle) ? null : item.UrlSubtitle,
            DurationSeconds = item.Duration,
            SizeBytes = item.Size is > 0 ? item.Size.Value : fileSize,
            Timestamp = timestamp,
            Description = string.IsNullOrEmpty(item.Description) ? null : item.Description,
            Quality = quality,
            QualityInfo = pattern?.Resolution is not null
                ? new QualityInfo
                {
                    Resolution = pattern.Resolution.Value,
                    Codec = codec ?? "h264",
                    BitrateKbps = pattern.BitrateKbps,
                    FileSize = item.Size is > 0 ? item.Size.Value : fileSize,
                    Container = UrlPatternAnalyzer.IsHls(url) ? "m3u8" : "mp4",
                    ProbeSource = ProbeSource.UrlPattern,
                }
                : null,
        });
    }

    private static QualityTier ResolutionToTier(Resolution resolution) => resolution.Height switch
    {
        >= 1080 => QualityTier.HD1080,
        >= 720 => QualityTier.HD720,
        _ => QualityTier.SD,
    };

    private static long? EstimateSizeFromPattern(UrlPatternResult? pattern, int durationSeconds) =>
        pattern?.BitrateKbps is > 0
            ? (long)durationSeconds * pattern.BitrateKbps.Value * 1000 / 8
            : null;

    private static long EstimateSize(int durationSeconds, QualityTier quality)
    {
        var bitrateKbps = quality switch
        {
            QualityTier.HD1080 => 4000,
            QualityTier.HD720 => 2000,
            _ => 800,
        };
        return (long)durationSeconds * bitrateKbps * 1000 / 8;
    }
}
