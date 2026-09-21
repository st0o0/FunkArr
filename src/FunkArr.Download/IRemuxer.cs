namespace FunkArr.Download;

public interface IRemuxer
{
    Task<FfmpegResult> RunAsync(
        string videoUrl, string? subtitleUrl, string outputPath,
        Action<ProgressUpdate> onProgress, CancellationToken ct);
}
