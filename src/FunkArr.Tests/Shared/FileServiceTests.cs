using FunkArr.Shared;

namespace FunkArr.Tests.Shared;

public class FileServiceTests
{
    private readonly FileService _sut = new();

    [Fact]
    public void GetTempVideoPath_CombinesPathAndNzoId()
    {
        var result = _sut.GetTempVideoPath("data/temp", "abc123");

        Assert.Equal(Path.Combine("data/temp", "abc123.mp4"), result);
    }

    [Fact]
    public void GetTempSubtitlePath_CombinesPathAndNzoId()
    {
        var result = _sut.GetTempSubtitlePath("data/temp", "abc123");

        Assert.Equal(Path.Combine("data/temp", "abc123.srt"), result);
    }

    [Fact]
    public void GetOutputPath_CreatesNestedStructure()
    {
        var result = _sut.GetOutputPath("/media/downloads", "My Show S01E03");

        Assert.Equal(Path.Combine("/media/downloads", "My Show S01E03", "My Show S01E03.mkv"), result);
    }

    [Fact]
    public void EnsureDirectoriesExist_CreatesDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}", "temp");
        var dlDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}", "downloads");

        try
        {
            _sut.EnsureDirectoriesExist(tempDir, dlDir);

            Assert.True(Directory.Exists(tempDir));
            Assert.True(Directory.Exists(dlDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }

            if (Directory.Exists(dlDir))
            {
                Directory.Delete(dlDir, true);
            }
        }
    }

    [Fact]
    public void CleanupTempFiles_DeletesExistingFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var videoPath = Path.Combine(dir, "test.mp4");
        var subPath = Path.Combine(dir, "test.srt");
        File.WriteAllText(videoPath, "video");
        File.WriteAllText(subPath, "subtitle");

        try
        {
            _sut.CleanupTempFiles(videoPath, subPath);

            Assert.False(File.Exists(videoPath));
            Assert.False(File.Exists(subPath));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void CleanupTempFiles_ToleratesMissingFiles()
    {
        var ex = Record.Exception(() =>
            _sut.CleanupTempFiles("/nonexistent/path.mp4", "/also/nonexistent.srt"));

        Assert.Null(ex);
    }

    [Fact]
    public void CleanupTempFiles_SkipsNullPaths()
    {
        var ex = Record.Exception(() =>
            _sut.CleanupTempFiles("/nonexistent/path.mp4", null, null));

        Assert.Null(ex);
    }
}
