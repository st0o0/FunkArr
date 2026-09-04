using System.Diagnostics;
using System.Globalization;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Exceptions;

namespace FunkArr.Download;

internal sealed class FfmpegRunner : IFfmpegRunner
{
    public async Task<FfmpegResult> RunAsync(
        string videoUrl, string? subtitleUrl, string outputPath,
        Action<ProgressUpdate> onProgress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var progressBlock = new Dictionary<string, string>();

        var processor = BuildArguments(videoUrl, subtitleUrl, outputPath)
            .NotifyOnOutput(line => ParseProgressLine(line, progressBlock, onProgress))
            .CancellableThrough(ct);

        try
        {
            await processor.ProcessAsynchronously(throwOnError: true);
            sw.Stop();
            return new FfmpegResult(true, 0, null, (int)sw.Elapsed.TotalSeconds);
        }
        catch (FFMpegException ex) when (subtitleUrl is not null && IsSubtitleInputError(ex))
        {
            sw.Stop();
            var retryProcessor = BuildArguments(videoUrl, null, outputPath)
                .NotifyOnOutput(line => ParseProgressLine(line, progressBlock, onProgress))
                .CancellableThrough(ct);

            var retrySw = Stopwatch.StartNew();
            try
            {
                await retryProcessor.ProcessAsynchronously(throwOnError: true);
                retrySw.Stop();
                return new FfmpegResult(true, 0, null, (int)(sw.Elapsed + retrySw.Elapsed).TotalSeconds);
            }
            catch (FFMpegException retryEx)
            {
                retrySw.Stop();
                return new FfmpegResult(false, 1, CapStderr(retryEx.FFMpegErrorOutput), (int)(sw.Elapsed + retrySw.Elapsed).TotalSeconds);
            }
        }
        catch (FFMpegException ex)
        {
            sw.Stop();
            return new FfmpegResult(false, 1, CapStderr(ex.FFMpegErrorOutput), (int)sw.Elapsed.TotalSeconds);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new FfmpegResult(false, -1, "Cancelled", (int)sw.Elapsed.TotalSeconds);
        }
    }

    internal static FFMpegArgumentProcessor BuildArguments(
        string videoUrl, string? subtitleUrl, string outputPath)
    {
        var arguments = subtitleUrl is not null
            ? FFMpegArguments
                .FromUrlInput(new Uri(videoUrl))
                .AddUrlInput(new Uri(subtitleUrl))
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

    private static bool IsSubtitleInputError(FFMpegException ex) =>
        ex.FFMpegErrorOutput?.Contains("Error opening input file", StringComparison.OrdinalIgnoreCase) == true ||
        ex.FFMpegErrorOutput?.Contains("Invalid data found when processing input", StringComparison.OrdinalIgnoreCase) == true;

    private static string CapStderr(string? stderr)
        => stderr is null or { Length: <= 4096 } ? stderr ?? "" : stderr[^4096..];
}
