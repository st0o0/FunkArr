namespace FunkArr.Download.Subtitles;

internal interface ISubtitleDownloader
{
    Task<SubtitleResult> DownloadAsync(string url, string outputDirectory, string routeName, CancellationToken ct);
}
