using System.Diagnostics;
using System.Text.Json;
using FunkArr.Shared;

namespace FunkArr.DownloadClient.Ffmpeg;

public sealed class FfmpegService : IFfmpegService
{
    private const string FfmpegBinary = "ffmpeg";
    private const string FfprobeBinary = "ffprobe";
    private static readonly TimeSpan HlsTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RemuxTimeout = TimeSpan.FromSeconds(600);
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ExtractTimeout = TimeSpan.FromSeconds(120);

    private readonly IFileService _fileService;
    private readonly ILogger<FfmpegService> _log;

    public FfmpegService(IFileService fileService, ILogger<FfmpegService> log)
    {
        _fileService = fileService;
        _log = log;
    }

    public async Task DownloadHlsAsync(string nzoId, string url, CancellationToken ct = default)
    {
        var outputPath = _fileService.GetTempVideoPath(nzoId);
        var args = BuildHlsDownloadArgs(url, outputPath);
        _log.LogInformation("Starting HLS download for {NzoId}: {Url}", nzoId, url);
        await RunFfmpegAsync(args, HlsTimeout, ct);
        _log.LogInformation("HLS download completed for {NzoId}", nzoId);
    }

    public async Task<bool> HasSubtitleStreamAsync(string manifestUrl, CancellationToken ct = default)
    {
        try
        {
            var args = $"-v quiet -print_format json -show_streams \"{manifestUrl}\"";
            var (exitCode, stdout, _) = await RunProcessAsync(FfprobeBinary, args, ProbeTimeout, ct);

            if (exitCode != 0)
            {
                return false;
            }

            using var doc = JsonDocument.Parse(stdout);
            var streams = doc.RootElement.GetProperty("streams");
            foreach (var stream in streams.EnumerateArray())
            {
                if (stream.TryGetProperty("codec_type", out var codecType)
                    && codecType.GetString() == "subtitle")
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogWarning(ex, "ffprobe failed for {Url}", manifestUrl);
            return false;
        }
    }

    public async Task<bool> ExtractSubtitleAsync(string nzoId, string manifestUrl, CancellationToken ct = default)
    {
        if (!await HasSubtitleStreamAsync(manifestUrl, ct))
        {
            return false;
        }

        try
        {
            var outputPath = _fileService.GetTempSubtitlePath(nzoId, ".srt");
            var args = $"-i \"{manifestUrl}\" -map 0:s:0 -c:s srt -y \"{outputPath}\"";
            var (exitCode, _, _) = await RunProcessAsync(FfmpegBinary, args, ExtractTimeout, ct);

            if (exitCode != 0)
            {
                _log.LogWarning("Failed to extract subtitle from HLS for {NzoId}", nzoId);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogWarning(ex, "Subtitle extraction failed for {NzoId}", nzoId);
            return false;
        }
    }

    public async Task<string> RemuxAsync(string nzoId, string title, bool hasSubtitle, string? category = null, CancellationToken ct = default)
    {
        var videoPath = _fileService.GetTempVideoPath(nzoId);
        var subtitlePath = hasSubtitle ? _fileService.GetNormalizedSubtitlePath(nzoId) : null;
        _fileService.EnsureOutputDirectory(title, category);
        var outputPath = _fileService.GetOutputPath(title, category);
        var args = BuildRemuxArgs(videoPath, subtitlePath, outputPath);

        _log.LogInformation("Starting muxing for {NzoId}: {Args}", nzoId, args);
        await RunFfmpegAsync(args, RemuxTimeout, ct);
        _fileService.CleanupTemp(nzoId);
        _log.LogInformation("Muxing completed for {NzoId}: {Path}", nzoId, outputPath);

        return outputPath;
    }

    internal static string BuildHlsDownloadArgs(string url, string outputPath)
        => $"-i \"{url}\" -map 0:v -map 0:a -c copy -y \"{outputPath}\"";

    internal static string BuildRemuxArgs(string videoPath, string? subtitlePath, string outputPath)
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

    private async Task RunFfmpegAsync(string args, TimeSpan timeout, CancellationToken ct)
    {
        var (exitCode, _, stderr) = await RunProcessAsync(FfmpegBinary, args, timeout, ct);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"FFmpeg exited with code {exitCode}: {stderr}");
        }
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessAsync(
        string binary, string args, TimeSpan timeout, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = binary,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (process.ExitCode, stdout, stderr);
    }
}
