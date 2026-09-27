namespace FunkArr.Download;

internal interface ISubtitleDownloader
{
    Task<SubtitleResult> DownloadAsync(string url, string outputDirectory, string routeName, CancellationToken ct);
}
