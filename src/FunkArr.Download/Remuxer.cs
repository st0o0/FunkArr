namespace FunkArr.Download;

internal sealed class Remuxer(ISubtitlePreparer subtitlePreparer, IFfmpegRunner ffmpeg) : IRemuxer
{
    public async Task<FfmpegResult> RunAsync(
        string videoUrl, string? subtitleUrl, string outputPath,
        string routeName, string? proxyUrl, Action<ProgressUpdate> onProgress, CancellationToken ct)
    {
        using var activity = Telemetry.Source.StartActivity("download.remux");
        string? subtitlePath = null;
        try
        {
            if (subtitleUrl is not null)
            {
                subtitlePath = await subtitlePreparer.PrepareAsync(subtitleUrl, Path.GetDirectoryName(outputPath)!, routeName, ct);
            }

            return await ffmpeg.RunAsync(videoUrl, subtitlePath, outputPath, proxyUrl, onProgress, ct);
        }
        finally
        {
            if (subtitlePath is not null)
            {
                try
                {
                    File.Delete(subtitlePath);
                }
                catch
                {
                    /* cleanup is best-effort */
                }
            }
        }
    }
}
