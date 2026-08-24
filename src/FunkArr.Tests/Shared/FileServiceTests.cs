using System.IO.Abstractions.TestingHelpers;
using FunkArr.Configuration;
using FunkArr.Shared;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.Shared;

public class FileServiceTests
{
    private static FileService CreateService(
        MockFileSystem? fs = null,
        string tempPath = "data/temp",
        string downloadPath = "/media/downloads",
        Dictionary<string, string>? category = null)
    {
        fs ??= new MockFileSystem();
        var opts = new DownloadOptions { TempPath = tempPath, Path = downloadPath };
        if (category is not null)
        {
            opts.Category = category;
        }

        var options = Options.Create(opts);
        return new FileService(fs, options);
    }

    [Fact]
    public void GetTempVideoPath_CombinesPathAndNzoId()
    {
        var sut = CreateService();
        var result = sut.GetTempVideoPath("abc123");
        Assert.Equal(Path.Combine("data/temp", "abc123.mp4"), result);
    }

    [Fact]
    public void GetTempSubtitlePath_DefaultExtension_UsesSub()
    {
        var sut = CreateService();
        var result = sut.GetTempSubtitlePath("abc123");
        Assert.Equal(Path.Combine("data/temp", "abc123.sub"), result);
    }

    [Fact]
    public void GetTempSubtitlePath_WithExtension_UsesProvidedExtension()
    {
        var sut = CreateService();
        var result = sut.GetTempSubtitlePath("abc123", ".vtt");
        Assert.Equal(Path.Combine("data/temp", "abc123.vtt"), result);
    }

    [Fact]
    public void GetNormalizedSubtitlePath_AlwaysReturnsSrt()
    {
        var sut = CreateService();
        var result = sut.GetNormalizedSubtitlePath("abc123");
        Assert.Equal(Path.Combine("data/temp", "abc123.srt"), result);
    }

    [Fact]
    public void GetOutputPath_CreatesNestedStructure()
    {
        var sut = CreateService();
        var result = sut.GetOutputPath("My Show S01E03");
        Assert.Equal(Path.Combine("/media/downloads", "My Show S01E03", "My Show S01E03.mkv"), result);
    }

    [Fact]
    public void EnsureDirectoriesExist_CreatesDirectories()
    {
        var fs = new MockFileSystem();
        var sut = CreateService(fs);

        sut.EnsureDirectoriesExist();

        Assert.True(fs.Directory.Exists("data/temp"));
        Assert.True(fs.Directory.Exists("/media/downloads"));
    }

    [Fact]
    public void EnsureOutputDirectory_CreatesSubdirectory()
    {
        var fs = new MockFileSystem();
        var sut = CreateService(fs);

        sut.EnsureOutputDirectory("My Show S01E03");

        Assert.True(fs.Directory.Exists(Path.Combine("/media/downloads", "My Show S01E03")));
    }

    [Fact]
    public void CleanupTemp_DeletesMatchingFiles()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("data/temp");
        fs.File.WriteAllText(Path.Combine("data/temp", "abc123.mp4"), "video");
        fs.File.WriteAllText(Path.Combine("data/temp", "abc123.srt"), "subtitle");
        fs.File.WriteAllText(Path.Combine("data/temp", "other.mp4"), "keep");
        var sut = CreateService(fs);

        sut.CleanupTemp("abc123");

        Assert.False(fs.File.Exists(Path.Combine("data/temp", "abc123.mp4")));
        Assert.False(fs.File.Exists(Path.Combine("data/temp", "abc123.srt")));
        Assert.True(fs.File.Exists(Path.Combine("data/temp", "other.mp4")));
    }

    [Fact]
    public void CleanupTemp_ToleratesMissingDirectory()
    {
        var sut = CreateService();
        var ex = Record.Exception(() => sut.CleanupTemp("nonexistent"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task SaveVideoAsync_WritesStreamToTempPath()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("data/temp");
        var sut = CreateService(fs);
        var content = new MemoryStream("video-content"u8.ToArray());

        await sut.SaveVideoAsync("abc123", content);

        var written = fs.File.ReadAllText(Path.Combine("data/temp", "abc123.mp4"));
        Assert.Equal("video-content", written);
    }

    [Fact]
    public async Task SaveSubtitleAsync_WritesBytes()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("data/temp");
        var sut = CreateService(fs);
        var content = "subtitle-content"u8.ToArray();

        await sut.SaveSubtitleAsync("abc123", content, ".vtt");

        var written = fs.File.ReadAllBytes(Path.Combine("data/temp", "abc123.vtt"));
        Assert.Equal(content, written);
    }

    [Fact]
    public void GetOutputPath_WithConfiguredCategory_ResolvesViaCategory()
    {
        var category = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["tv"] = "serien" };
        var sut = CreateService(downloadPath: "/downloads", category: category);

        var result = sut.GetOutputPath("My Show S01E03", "tv");

        Assert.Equal(Path.Combine("/downloads", "serien", "My Show S01E03", "My Show S01E03.mkv"), result);
    }

    [Fact]
    public void GetOutputPath_WithUnknownCategory_UsesCategoryAsSubfolder()
    {
        var sut = CreateService(downloadPath: "/downloads");

        var result = sut.GetOutputPath("My Show S01E03", "anime");

        Assert.Equal(Path.Combine("/downloads", "anime", "My Show S01E03", "My Show S01E03.mkv"), result);
    }

    [Fact]
    public void EnsureOutputDirectory_WithCategory_CreatesCorrectSubdirectory()
    {
        var fs = new MockFileSystem();
        var category = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["tv"] = "serien" };
        var sut = CreateService(fs, downloadPath: "/downloads", category: category);

        sut.EnsureOutputDirectory("My Show S01E03", "tv");

        Assert.True(fs.Directory.Exists(Path.Combine("/downloads", "serien", "My Show S01E03")));
    }
}
