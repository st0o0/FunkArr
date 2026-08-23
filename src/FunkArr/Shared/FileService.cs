namespace FunkArr.Shared;

public sealed class FileService : IFileService
{
    public void EnsureDirectoriesExist(string tempPath, string downloadPath)
    {
        Directory.CreateDirectory(tempPath);
        Directory.CreateDirectory(downloadPath);
    }

    public string GetTempVideoPath(string tempPath, string nzoId)
        => Path.Combine(tempPath, $"{nzoId}.mp4");

    public string GetTempSubtitlePath(string tempPath, string nzoId, string extension = ".sub")
        => Path.Combine(tempPath, $"{nzoId}{extension}");

    public string GetNormalizedSubtitlePath(string tempPath, string nzoId)
        => Path.Combine(tempPath, $"{nzoId}.srt");

    public string GetOutputPath(string downloadPath, string title)
        => Path.Combine(downloadPath, title, $"{title}.mkv");

    public void EnsureOutputDirectory(string downloadPath, string title)
        => Directory.CreateDirectory(Path.Combine(downloadPath, title));

    public void CleanupTempFiles(string videoPath, params string?[] additionalPaths)
    {
        TryDelete(videoPath);
        foreach (var path in additionalPaths)
        {
            if (path is not null)
            {
                TryDelete(path);
            }
        }
    }

    public Task WriteSubtitleAsync(string path, byte[] content)
        => File.WriteAllBytesAsync(path, content);

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch { /* preserve on failure */ }
    }
}
