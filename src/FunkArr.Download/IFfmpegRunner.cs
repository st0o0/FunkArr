namespace FunkArr.Download;

public sealed record ProgressUpdate(long TotalSize, long OutTimeUs, double Speed);

public sealed record FfmpegResult(bool Success, int ExitCode, string? Error, int ElapsedSeconds);

public interface IFfmpegRunner
{
    Task<FfmpegResult> RunAsync(
        string videoUrl, string? subtitleUrl, string outputPath,
        Action<ProgressUpdate> onProgress, CancellationToken ct);
}
