namespace FunkArr.Download;

internal sealed class Remuxer(ISubtitlePreparer subtitlePreparer, IFfmpegRunner ffmpeg) : IRemuxer
{
    public async Task<FfmpegResult> RunAsync(
        string videoUrl, string? subtitleUrl, string outputPath,
        Action<ProgressUpdate> onProgress, CancellationToken ct)
    {
        string? subtitlePath = null;
        try
        {
            if (subtitleUrl is not null)
                subtitlePath = await subtitlePreparer.PrepareAsync(subtitleUrl, Path.GetDirectoryName(outputPath)!, ct);

            return await ffmpeg.RunAsync(videoUrl, subtitlePath, outputPath, onProgress, ct);
        }
        finally
        {
            if (subtitlePath is not null)
            {
                try { File.Delete(subtitlePath); }
                catch { /* cleanup is best-effort */ }
            }
        }
    }
}
