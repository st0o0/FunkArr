using System.Diagnostics;
using System.Globalization;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Exceptions;

namespace FunkArr.Download;

internal sealed class FfmpegRunner : IFfmpegRunner
{
    public async Task<FfmpegResult> RunAsync(
        string videoUrl, string? subtitlePath, string outputPath,
        Action<ProgressUpdate> onProgress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var progressBlock = new Dictionary<string, string>();

        var processor = BuildArguments(videoUrl, subtitlePath, outputPath)
            .NotifyOnOutput(line => ParseProgressLine(line, progressBlock, onProgress))
            .CancellableThrough(ct);

        try
        {
            await processor.ProcessAsynchronously(throwOnError: true);
            sw.Stop();
            return new FfmpegResult(true, 0, null, (int)sw.Elapsed.TotalSeconds);
        }
        catch (FFMpegException ex)
        {
            sw.Stop();
            return new FfmpegResult(false, 1, ExtractError(ex.FFMpegErrorOutput), (int)sw.Elapsed.TotalSeconds);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new FfmpegResult(false, -1, "Cancelled", (int)sw.Elapsed.TotalSeconds);
        }
    }

    internal static FFMpegArgumentProcessor BuildArguments(
        string videoUrl, string? subtitlePath, string outputPath)
    {
        var arguments = subtitlePath is not null
            ? FFMpegArguments
                .FromUrlInput(new Uri(videoUrl))
                .AddFileInput(subtitlePath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .CopyChannel(Channel.Video)
                    .CopyChannel(Channel.Audio)
                    .WithCustomArgument("-c:s srt")
                    .WithCustomArgument("-metadata:s:s:0 language=deu")
                    .WithCustomArgument("-progress pipe:1"))
            : FFMpegArguments
                .FromUrlInput(new Uri(videoUrl))
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithCopyCodec()
                    .WithCustomArgument("-progress pipe:1"));

        return arguments;
    }

    internal static void ParseProgressLine(
        string line, Dictionary<string, string> block, Action<ProgressUpdate> onProgress)
    {
        var eqIndex = line.IndexOf('=');
        if (eqIndex <= 0)
        {
            return;
        }

        var key = line[..eqIndex].Trim();
        var value = line[(eqIndex + 1)..].Trim();
        block[key] = value;

        if (key != "progress")
        {
            return;
        }

        if (block.Count > 0)
        {
            var totalSize = GetLong(block, "total_size");
            var outTimeUs = GetLong(block, "out_time_us");
            var speed = ParseSpeed(block.GetValueOrDefault("speed"));
            onProgress(new ProgressUpdate(totalSize, outTimeUs, speed));
        }

        block.Clear();
    }

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

    internal static string ExtractError(string? stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
        {
            return "";
        }

        var lines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (line.Contains("HTTP error", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Error opening input", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Server returned", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Invalid data found", StringComparison.OrdinalIgnoreCase))
            {
                return line;
            }
        }

        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (line.Length > 0)
            {
                return line;
            }
        }

        return "";
    }
}
