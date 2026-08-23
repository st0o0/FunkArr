using System.Text.RegularExpressions;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

public static partial class HlsManifestParser
{
    public static QualityInfo? Parse(string manifestContent, int durationSeconds)
    {
        Resolution? bestResolution = null;
        var bestBandwidth = 0;

        foreach (Match match in StreamInfPattern().Matches(manifestContent))
        {
            var attributes = match.Groups[1].Value;

            var bandwidthMatch = BandwidthPattern().Match(attributes);
            var bandwidth = bandwidthMatch.Success ? int.Parse(bandwidthMatch.Groups[1].Value) : 0;

            var resolutionMatch = ResolutionPattern().Match(attributes);
            if (resolutionMatch.Success && bandwidth >= bestBandwidth)
            {
                var width = int.Parse(resolutionMatch.Groups[1].Value);
                var height = int.Parse(resolutionMatch.Groups[2].Value);
                bestResolution = new Resolution(width, height);
                bestBandwidth = bandwidth;
            }
            else if (bandwidth > bestBandwidth)
            {
                bestBandwidth = bandwidth;
            }
        }

        if (bestResolution is null && bestBandwidth == 0)
        {
            return null;
        }

        var bitrateKbps = bestBandwidth / 1000;
        var resolution = bestResolution ?? EstimateResolutionFromBandwidth(bitrateKbps);
        var fileSize = durationSeconds > 0 ? (long)durationSeconds * bestBandwidth / 8 : 0;

        return new QualityInfo
        {
            Resolution = resolution,
            Codec = "h264",
            BitrateKbps = bitrateKbps,
            FileSize = fileSize,
            Container = "m3u8",
            ProbeSource = ProbeSource.HlsManifest,
        };
    }

    private static Resolution EstimateResolutionFromBandwidth(int bitrateKbps) => bitrateKbps switch
    {
        > 4000 => new Resolution(1920, 1080),
        > 2000 => new Resolution(1280, 720),
        _ => new Resolution(640, 480),
    };

    [GeneratedRegex(@"#EXT-X-STREAM-INF:(.+)")]
    private static partial Regex StreamInfPattern();

    [GeneratedRegex(@"BANDWIDTH=(\d+)")]
    private static partial Regex BandwidthPattern();

    [GeneratedRegex(@"RESOLUTION=(\d+)x(\d+)")]
    private static partial Regex ResolutionPattern();
}
