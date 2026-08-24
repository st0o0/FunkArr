using System.IO.Abstractions;
using FunkArr.Configuration;
using FunkArr.Subtitle;
using Microsoft.Extensions.Options;

namespace FunkArr.Shared;

public sealed class FileService : IFileService
{
    private readonly IFileSystem _fs;
    private readonly string _tempPath;
    private readonly string _downloadPath;
    private readonly Dictionary<string, string> _categoryConfig;

    public FileService(IFileSystem fileSystem, IOptions<DownloadOptions> options)
    {
        _fs = fileSystem;
        _tempPath = options.Value.TempPath;
        _downloadPath = options.Value.Path;
        _categoryConfig = options.Value.Category;
    }

    public void EnsureDirectoriesExist()
    {
        _fs.Directory.CreateDirectory(_tempPath);
        _fs.Directory.CreateDirectory(_downloadPath);
    }

    public string GetTempVideoPath(string nzoId)
        => _fs.Path.Combine(_tempPath, $"{nzoId}.mp4");

    public string GetTempSubtitlePath(string nzoId, string extension = ".sub")
        => _fs.Path.Combine(_tempPath, $"{nzoId}{extension}");

    public string GetNormalizedSubtitlePath(string nzoId)
        => _fs.Path.Combine(_tempPath, $"{nzoId}.srt");

    public string GetOutputPath(string title, string? category = null)
    {
        var basePath = CategoryResolver.Resolve(_downloadPath, category, _categoryConfig);
        return _fs.Path.Combine(basePath, title, $"{title}.mkv");
    }

    public void EnsureOutputDirectory(string title, string? category = null)
    {
        var basePath = CategoryResolver.Resolve(_downloadPath, category, _categoryConfig);
        _fs.Directory.CreateDirectory(_fs.Path.Combine(basePath, title));
    }

    public void CleanupTemp(string nzoId)
    {
        var pattern = $"{nzoId}.*";
        if (!_fs.Directory.Exists(_tempPath))
        {
            return;
        }

        foreach (var file in _fs.Directory.GetFiles(_tempPath, pattern))
        {
            TryDelete(file);
        }
    }

    public async Task SaveVideoAsync(string nzoId, Stream content)
    {
        var path = GetTempVideoPath(nzoId);
        var buffer = new byte[8192];

        await using var fileStream = _fs.FileStream.New(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        int bytesRead;
        while ((bytesRead = await content.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
        }
    }

    public async Task SaveSubtitleAsync(string nzoId, byte[] content, string extension)
    {
        var path = GetTempSubtitlePath(nzoId, extension);
        await _fs.File.WriteAllBytesAsync(path, content);
    }

    public async Task<string?> NormalizeSubtitleAsync(string nzoId)
    {
        var subtitlePath = FindTempSubtitleFile(nzoId);
        if (subtitlePath is null)
        {
            return null;
        }

        var outputPath = GetNormalizedSubtitlePath(nzoId);
        return await SubtitleNormalizer.NormalizeAsync(subtitlePath, outputPath);
    }

    private string? FindTempSubtitleFile(string nzoId)
    {
        if (!_fs.Directory.Exists(_tempPath))
        {
            return null;
        }

        string[] extensions = [".sub", ".vtt", ".ttml", ".srt"];
        foreach (var ext in extensions)
        {
            var path = GetTempSubtitlePath(nzoId, ext);
            if (_fs.File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private void TryDelete(string path)
    {
        try { _fs.File.Delete(path); }
        catch { /* preserve on failure */ }
    }
}
