namespace FunkArr.Shared;

public interface IFileService
{
    void EnsureDirectoriesExist(string tempPath, string downloadPath);
    string GetTempVideoPath(string tempPath, string nzoId);
    string GetTempSubtitlePath(string tempPath, string nzoId, string extension = ".sub");
    string GetNormalizedSubtitlePath(string tempPath, string nzoId);
    string GetOutputPath(string downloadPath, string title);
    void EnsureOutputDirectory(string downloadPath, string title);
    void CleanupTempFiles(string videoPath, params string?[] additionalPaths);
    Task WriteSubtitleAsync(string path, byte[] content);
}
