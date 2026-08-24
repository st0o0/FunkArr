using System.Text.RegularExpressions;
using FunkArr.Shared.Models;

namespace FunkArr.Search.Quality;

public static partial class UrlPatternAnalyzer
{
    public static UrlPatternResult? Analyze(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        return TryZdf(url) ?? TryArd(url) ?? TryArte(url);
    }

    public static bool IsHls(string url) =>
        url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase);

    public static bool IsDash(string url) =>
        url.EndsWith(".mpd", StringComparison.OrdinalIgnoreCase);

    public static bool IsNonProbeable(string url) =>
        IsHls(url) || IsDash(url);

    private static UrlPatternResult? TryZdf(string url)
    {
        var match = ZdfPattern().Match(url);
        if (!match.Success)
        {
            return null;
        }

        var bitrate = int.Parse(match.Groups[1].Value);
        var profile = match.Groups[2].Value;

        var (width, height, codec) = MapZdfProfile(profile);

        return new UrlPatternResult
        {
            Resolution = new Resolution(width, height),
            Codec = codec,
            BitrateKbps = bitrate,
        };
    }

    private static (int Width, int Height, string Codec) MapZdfProfile(string profile) => profile switch
    {
        "p11" => (640, 360, "h264"),
        "p13" => (960, 540, "h264"),
        "p14" => (1024, 576, "h264"),
        "p15" => (1280, 720, "h264"),
        "p18" => (1280, 720, "h264"),
        "p35" => (1280, 720, "h265"),
        "p36" => (1280, 720, "h265"),
        "p37" => (1920, 1080, "h265"),
        _ => (1280, 720, "h264"),
    };

    private static UrlPatternResult? TryArd(string url)
    {
        var match = ArdResolutionPattern().Match(url);
        if (!match.Success)
        {
            return null;
        }

        var height = int.Parse(match.Groups[1].Value);
        var width = height switch
        {
            1080 => 1920,
            720 => 1280,
            480 => 854,
            360 => 640,
            _ => (int)(height * 16.0 / 9),
        };

        return new UrlPatternResult
        {
            Resolution = new Resolution(width, height),
        };
    }

    private static UrlPatternResult? TryArte(string url)
    {
        var match = ArteResolutionPattern().Match(url);
        if (!match.Success)
        {
            return null;
        }

        var height = int.Parse(match.Groups[1].Value);
        var width = height switch
        {
            1080 => 1920,
            720 => 1280,
            480 => 854,
            360 => 640,
            _ => (int)(height * 16.0 / 9),
        };

        return new UrlPatternResult
        {
            Resolution = new Resolution(width, height),
        };
    }

    [GeneratedRegex(@"(\d{3,5})k_(p\w+?)v\d+", RegexOptions.IgnoreCase)]
    private static partial Regex ZdfPattern();

    [GeneratedRegex(@"/(360|480|540|576|720|1080)/")]
    private static partial Regex ArdResolutionPattern();

    [GeneratedRegex(@"[_/](\d{3,4})p?[_/.]", RegexOptions.IgnoreCase)]
    private static partial Regex ArteResolutionPattern();
}

public sealed record UrlPatternResult
{
    public Resolution? Resolution { get; init; }
    public string? Codec { get; init; }
    public int? BitrateKbps { get; init; }
}
