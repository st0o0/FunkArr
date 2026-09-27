namespace FunkArr.Download;

internal interface ISubtitlePreparer
{
    Task<SubtitleResult> PrepareAsync(string url, string outputDirectory, string routeName, CancellationToken ct);
}
