using FunkArr.Core;
using Microsoft.Extensions.Logging;

namespace FunkArr.Download;

internal sealed class Remuxer(
    IRouteResolver routeResolver,
    ISubtitleDownloader subtitleDownloader,
    IFfmpegProcess ffmpegProcess,
    ILogger<Remuxer> logger) : IRemuxer
{
    public async Task<FfmpegResult> RunAsync(
        RemuxOptions options, Action<ProgressUpdate> onProgress, CancellationToken ct)
    {
        var route = options.Channel is not null
            ? routeResolver.Resolve(options.Channel)
            : new ResolvedRoute("Direct", null);

        string? subtitlePath = null;
        string? subtitleLanguage = null;
        try
        {
            if (options.SubtitleUrl is not null)
            {
                var result = await subtitleDownloader.DownloadAsync(
                    options.SubtitleUrl, Path.GetDirectoryName(options.OutputPath)!, route.Name, ct);
                switch (result)
                {
                    case SubtitleResult.Succeeded s:
                        subtitlePath = s.FilePath;
                        subtitleLanguage = s.Track.Language;
                        break;
                    case SubtitleResult.Failed f:
                        logger.LogWarning("Subtitle failed for {Url}: {Reason} {Detail}",
                            options.SubtitleUrl, f.Reason, f.Detail);
                        break;
                }
            }

            var input = new FfmpegInput(
                options.VideoUrl, subtitlePath, options.OutputPath,
                route.ProxyUrl, subtitleLanguage, options.IsHls);

            return await ffmpegProcess.ExecuteAsync(input, onProgress, ct);
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
