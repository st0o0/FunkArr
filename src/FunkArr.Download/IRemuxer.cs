namespace FunkArr.Download;

public interface IRemuxer
{
    Task<FfmpegResult> RunAsync(
        string videoUrl, string? subtitleUrl, string outputPath,
        string routeName, string? proxyUrl, Action<ProgressUpdate> onProgress, CancellationToken ct);
}
