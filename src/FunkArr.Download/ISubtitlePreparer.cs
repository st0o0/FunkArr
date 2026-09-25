namespace FunkArr.Download;

public interface ISubtitlePreparer
{
    Task<string?> PrepareAsync(string url, string outputDirectory, string routeName, CancellationToken ct);
}
