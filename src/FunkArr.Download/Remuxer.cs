using Microsoft.Extensions.Logging;

namespace FunkArr.Download;

internal sealed class Remuxer(ISubtitlePreparer subtitlePreparer, IFfmpegRunner ffmpeg, ILogger<Remuxer> logger) : IRemuxer
{
    public async Task<FfmpegResult> RunAsync(
        string videoUrl, string? subtitleUrl, string outputPath,
        string routeName, string? proxyUrl, Action<ProgressUpdate> onProgress, CancellationToken ct)
    {
        string? subtitlePath = null;
        string? subtitleLanguage = null;
        try
        {
            if (subtitleUrl is not null)
            {
                var result = await subtitlePreparer.PrepareAsync(subtitleUrl, Path.GetDirectoryName(outputPath)!, routeName, ct);
                switch (result)
                {
                    case SubtitleResult.Succeeded s:
                        subtitlePath = s.FilePath;
                        subtitleLanguage = s.Track.Language;
                        break;
                    case SubtitleResult.Failed f:
                        logger.LogWarning("Subtitle failed for {Url}: {Reason} {Detail}", subtitleUrl, f.Reason, f.Detail);
                        break;
                }
            }

            return await ffmpeg.RunAsync(videoUrl, subtitlePath, outputPath, proxyUrl, subtitleLanguage, onProgress, ct);
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
                }
            }
        }
    }
}
