using System.Globalization;

namespace FunkArr.Download;

internal static class FfmpegProgressParser
{
    public static ProgressUpdate? Parse(Dictionary<string, string> block)
    {
        if (block.Count == 0)
        {
            return null;
        }

        var outTimeUs = GetLong(block, "out_time_us");
        var totalSize = GetLong(block, "total_size");
        var speed = ParseSpeed(block.GetValueOrDefault("speed"));

        return new ProgressUpdate(totalSize, outTimeUs, speed);
    }

    public static Dictionary<string, string> AccumulateLine(Dictionary<string, string> block, string line)
    {
        var eqIndex = line.IndexOf('=');
        if (eqIndex <= 0)
        {
            return block;
        }

        var key = line[..eqIndex].Trim();
        var value = line[(eqIndex + 1)..].Trim();
        block[key] = value;

        return block;
    }

    public static bool IsBlockComplete(Dictionary<string, string> block) =>
        block.ContainsKey("progress");

    private static long GetLong(Dictionary<string, string> block, string key) =>
        block.TryGetValue(key, out var value) && long.TryParse(value, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;

    private static double ParseSpeed(string? value)
    {
        if (value is null or "N/A")
        {
            return 0.0;
        }

        var trimmed = value.TrimEnd('x');
        return double.TryParse(trimmed, CultureInfo.InvariantCulture, out var result) ? result : 0.0;
    }
}
