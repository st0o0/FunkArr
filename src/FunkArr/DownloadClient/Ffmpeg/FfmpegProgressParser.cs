using System.Text.RegularExpressions;

namespace FunkArr.DownloadClient.Ffmpeg;

public static partial class FfmpegProgressParser
{
    public static (long ElapsedSeconds, double Speed)? Parse(string line)
    {
        var timeMatch = TimePattern().Match(line);
        if (!timeMatch.Success)
        {
            return null;
        }

        var hours = int.Parse(timeMatch.Groups[1].Value);
        var minutes = int.Parse(timeMatch.Groups[2].Value);
        var seconds = int.Parse(timeMatch.Groups[3].Value);
        var elapsed = hours * 3600L + minutes * 60L + seconds;

        var speed = 0.0;
        var speedMatch = SpeedPattern().Match(line);
        if (speedMatch.Success)
        {
            double.TryParse(speedMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out speed);
        }

        return (elapsed, speed);
    }

    [GeneratedRegex(@"time=(\d{2}):(\d{2}):(\d{2})")]
    private static partial Regex TimePattern();

    [GeneratedRegex(@"speed=\s*([\d.]+)x")]
    private static partial Regex SpeedPattern();
}
