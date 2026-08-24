namespace FunkArr.Shared;

public interface IFileService
{
    void EnsureDirectoriesExist();
    string GetTempVideoPath(string nzoId);
    string GetTempSubtitlePath(string nzoId, string extension = ".sub");
    string GetNormalizedSubtitlePath(string nzoId);
    string GetOutputPath(string title, string? category = null);
    void EnsureOutputDirectory(string title, string? category = null);
    void CleanupTemp(string nzoId);
    Task SaveVideoAsync(string nzoId, Stream content);
    Task SaveSubtitleAsync(string nzoId, byte[] content, string extension);
    Task<string?> NormalizeSubtitleAsync(string nzoId);
}
