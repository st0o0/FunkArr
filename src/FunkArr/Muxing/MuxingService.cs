using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FunkArr.Shared;

namespace FunkArr.Muxing;

public sealed partial class MuxingService
{
    private const string FfmpegBinary = "ffmpeg";
    private static readonly TimeSpan MuxingTimeout = TimeSpan.FromSeconds(600);

    private readonly ILogger<MuxingService> _log;
    private readonly IFileService _fileService;

    public MuxingService(ILogger<MuxingService> log, IFileService fileService)
    {
        _log = log;
        _fileService = fileService;
    }

    public async Task<MuxOutcome> MuxAsync(
        string videoPath, string? subtitlePath,
        string outputDir, string title,
        CancellationToken ct = default)
    {
        var nzoId = Path.GetFileNameWithoutExtension(videoPath);
        _fileService.EnsureOutputDirectory(outputDir, title);
        var outputFile = _fileService.GetOutputPath(outputDir, title);

        try
        {
            var originalSubtitlePath = subtitlePath;
            if (subtitlePath is not null)
            {
                subtitlePath = await NormalizeSubtitleAsync(subtitlePath);
            }

            var args = BuildFfmpegArgs(videoPath, subtitlePath, outputFile);
            _log.LogInformation("Starting muxing for {NzoId}: {Args}", nzoId, args);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = FfmpegBinary,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync(ct);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(MuxingTimeout);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(true);
                return new MuxOutcome.Failure(nzoId, "Muxing timed out");
            }

            if (process.ExitCode != 0)
            {
                return new MuxOutcome.Failure(nzoId, $"FFmpeg exited with code {process.ExitCode}: {stderr}");
            }

            _fileService.CleanupTempFiles(videoPath, originalSubtitlePath,
                subtitlePath != originalSubtitlePath ? subtitlePath : null);
            return new MuxOutcome.Success(nzoId, outputFile);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Muxing failed for {NzoId}", nzoId);
            return new MuxOutcome.Failure(nzoId, ex.Message);
        }
    }

    internal static string BuildFfmpegArgs(string videoPath, string? subtitlePath, string outputPath)
    {
        if (subtitlePath is not null)
        {
            return $"-i \"{videoPath}\" -i \"{subtitlePath}\" " +
                   "-map 0:v -map 0:a -map 1:s " +
                   "-c copy -c:s srt " +
                   "-metadata:s:v:0 language=ger " +
                   "-metadata:s:a:0 language=ger " +
                   "-metadata:s:s:0 language=ger " +
                   $"-y \"{outputPath}\"";
        }

        return $"-i \"{videoPath}\" " +
               "-map 0:v -map 0:a " +
               "-c copy " +
               "-metadata:s:v:0 language=ger " +
               "-metadata:s:a:0 language=ger " +
               $"-y \"{outputPath}\"";
    }

    internal static async Task<string> NormalizeSubtitleAsync(string subtitlePath)
    {
        var extension = Path.GetExtension(subtitlePath).ToLowerInvariant();

        if (extension == ".srt")
        {
            return subtitlePath;
        }

        var content = await File.ReadAllTextAsync(subtitlePath);
        var srtPath = Path.ChangeExtension(subtitlePath, ".srt");

        if (extension == ".vtt")
        {
            var srtContent = ConvertVttToSrt(content);
            await File.WriteAllTextAsync(srtPath, srtContent);
            return srtPath;
        }

        if (extension is ".ttml" or ".xml")
        {
            var srtContent = ConvertTtmlToSrt(content);
            await File.WriteAllTextAsync(srtPath, srtContent);
            return srtPath;
        }

        return subtitlePath;
    }

    internal static string ConvertVttToSrt(string vttContent)
    {
        var lines = vttContent.Split('\n');
        var sb = new StringBuilder();
        var counter = 1;
        var inCue = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            if (line.StartsWith("WEBVTT") || line.StartsWith("NOTE") || line.StartsWith("STYLE"))
            {
                continue;
            }

            if (line.Contains("-->"))
            {
                sb.AppendLine(counter.ToString());
                sb.AppendLine(line.Replace('.', ','));
                inCue = true;
                continue;
            }

            if (inCue && string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine();
                counter++;
                inCue = false;
                continue;
            }

            if (inCue)
            {
                sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }

    internal static string ConvertTtmlToSrt(string ttmlContent)
    {
        var sb = new StringBuilder();
        var counter = 1;

        foreach (Match match in TtmlParagraphPattern().Matches(ttmlContent))
        {
            var begin = NormalizeTtmlTimestamp(match.Groups[1].Value);
            var end = NormalizeTtmlTimestamp(match.Groups[2].Value);
            var text = Regex.Replace(match.Groups[3].Value, @"<[^>]+>", "").Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            sb.AppendLine(counter.ToString());
            sb.AppendLine($"{begin} --> {end}");
            sb.AppendLine(text);
            sb.AppendLine();
            counter++;
        }

        return sb.ToString();
    }

    internal static string NormalizeTtmlTimestamp(string ts)
    {
        if (ts.Contains('.'))
        {
            return ts.Replace('.', ',');
        }

        if (!ts.Contains(','))
        {
            return ts + ",000";
        }

        return ts;
    }

    [GeneratedRegex("""<p[^>]*\sbegin="([^"]+)"[^>]*\send="([^"]+)"[^>]*>(.*?)</p>""", RegexOptions.Singleline)]
    private static partial Regex TtmlParagraphPattern();
}
