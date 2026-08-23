using System.Diagnostics;
using FunkArr.Shared;

namespace FunkArr.DownloadClient;

public sealed class HlsDownloadService(
    IFileService fileService,
    ILogger<HlsDownloadService> logger)
{
    private const string FfmpegBinary = "ffmpeg";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(30);

    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        long totalDurationSeconds,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var outputPath = fileService.GetTempVideoPath(request.TempPath, request.NzoId);
        var args = BuildFfmpegArgs(request.VideoUrl, outputPath);

        logger.LogInformation("Starting HLS download for {NzoId}: {Url}", request.NzoId, request.VideoUrl);

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

        var lastReport = DateTimeOffset.UtcNow;

        _ = Task.Run(async () =>
        {
            try
            {
                while (await process.StandardError.ReadLineAsync(cancellationToken) is { } line)
                {
                    var parsed = FfmpegProgressParser.Parse(line);
                    if (parsed is not null && DateTimeOffset.UtcNow - lastReport > TimeSpan.FromSeconds(2))
                    {
                        progress.Report(new DownloadProgress { DownloadedBytes = parsed.Value.ElapsedSeconds, TotalBytes = totalDurationSeconds });
                        lastReport = DateTimeOffset.UtcNow;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw;
        }

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException(
                $"FFmpeg HLS download failed with exit code {process.ExitCode}: {stderr}");
        }

        logger.LogInformation("HLS download completed for {NzoId}", request.NzoId);
        return new DownloadResult(outputPath, null);
    }

    internal static string BuildFfmpegArgs(string url, string outputPath)
        => $"-i \"{url}\" -map 0:v -map 0:a -c copy -y \"{outputPath}\"";
}
