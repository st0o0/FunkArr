namespace FunkArr.Download;

public interface ISubtitlePreparer
{
    Task<string?> PrepareAsync(string url, string outputDirectory, CancellationToken ct);
}
