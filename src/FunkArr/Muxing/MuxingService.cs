using System.Diagnostics;
using System.Diagnostics.Metrics;
using FunkArr.Diagnostics;
using FunkArr.Shared;

namespace FunkArr.Muxing;

public sealed class MuxingService
{
    private const string FfmpegBinary = "ffmpeg";
    private static readonly TimeSpan MuxingTimeout = TimeSpan.FromSeconds(600);

    private readonly ILogger<MuxingService> _log;
    private readonly IFileService _fileService;
    private readonly Histogram<double> _muxDuration = FunkArrMetrics.Instance.AddMuxDuration();

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
        var sw = Stopwatch.StartNew();

        try
        {
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
                _muxDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("outcome", "error"));
                return new MuxOutcome.Failure(nzoId, "Muxing timed out");
            }

            if (process.ExitCode != 0)
            {
                _muxDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("outcome", "error"));
                return new MuxOutcome.Failure(nzoId, $"FFmpeg exited with code {process.ExitCode}: {stderr}");
            }

            _fileService.CleanupTempFiles(videoPath, subtitlePath);
            _muxDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("outcome", "success"));
            return new MuxOutcome.Success(nzoId, outputFile);
        }
        catch (Exception ex)
        {
            _muxDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("outcome", "error"));
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
}
