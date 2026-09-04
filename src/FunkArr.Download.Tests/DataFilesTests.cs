using System.IO.Abstractions.TestingHelpers;
using FunkArr.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunkArr.Download.Tests;

public sealed class DataFilesTests
{
    private static (DataFiles sut, MockFileSystem fs) Create(MockFileSystem? fs = null)
    {
        fs ??= new MockFileSystem();
        var sut = new DataFiles(fs, NullLogger<DataFiles>.Instance);
        return (sut, fs);
    }

    [Fact]
    public void CreateDirectory_creates_directory()
    {
        var (sut, fs) = Create();

        sut.CreateDirectory("/data/rulesets/community");

        Assert.True(fs.Directory.Exists("/data/rulesets/community"));
    }

    [Fact]
    public void CreateDirectory_is_idempotent()
    {
        var (sut, fs) = Create();

        sut.CreateDirectory("/data/test");
        sut.CreateDirectory("/data/test");

        Assert.True(fs.Directory.Exists("/data/test"));
    }

    [Fact]
    public void Remove_deletes_file()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/data/test.txt"] = new("content"),
        });
        var (sut, _) = Create(fs);

        sut.Remove("/data/test.txt");

        Assert.False(fs.File.Exists("/data/test.txt"));
    }

    [Fact]
    public void Remove_deletes_directory_recursively()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/data/dir/sub/file.txt"] = new("content"),
        });
        var (sut, _) = Create(fs);

        sut.Remove("/data/dir");

        Assert.False(fs.Directory.Exists("/data/dir"));
    }

    [Fact]
    public void Remove_non_existent_does_not_throw()
    {
        var (sut, _) = Create();

        var ex = Record.Exception(() => sut.Remove("/nonexistent"));

        Assert.Null(ex);
    }

    [Fact]
    public void Move_moves_file()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/src/file.mkv"] = new("video"),
            ["/dest/placeholder"] = new(""),
        });
        var (sut, _) = Create(fs);

        sut.Move("/src/file.mkv", "/dest/file.mkv");

        Assert.False(fs.File.Exists("/src/file.mkv"));
        Assert.True(fs.File.Exists("/dest/file.mkv"));
        Assert.Equal("video", fs.File.ReadAllText("/dest/file.mkv"));
    }

    [Fact]
    public void Move_overwrites_destination()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/src/file.mkv"] = new("new"),
            ["/dest/file.mkv"] = new("old"),
        });
        var (sut, _) = Create(fs);

        sut.Move("/src/file.mkv", "/dest/file.mkv");

        Assert.Equal("new", fs.File.ReadAllText("/dest/file.mkv"));
    }

    [Fact]
    public void ReplaceDirectory_replaces_target()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/data/rulesets/community/old.json"] = new("old"),
            ["/data/rulesets-new/new.json"] = new("new"),
        });
        var (sut, _) = Create(fs);

        sut.ReplaceDirectory("/data/rulesets-new", "/data/rulesets/community");

        Assert.True(fs.File.Exists("/data/rulesets/community/new.json"));
        Assert.False(fs.File.Exists("/data/rulesets/community/old.json"));
        Assert.False(fs.Directory.Exists("/data/rulesets-new"));
    }

    [Fact]
    public void ReplaceDirectory_works_when_target_does_not_exist()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/data/rulesets-new/new.json"] = new("new"),
        });
        var (sut, _) = Create(fs);

        sut.ReplaceDirectory("/data/rulesets-new", "/data/rulesets/community");

        Assert.True(fs.File.Exists("/data/rulesets/community/new.json"));
    }

    [Fact]
    public void ReadText_returns_file_content()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/data/test.json"] = new("{\"key\":\"value\"}"),
        });
        var (sut, _) = Create(fs);

        var content = sut.ReadText("/data/test.json");

        Assert.Equal("{\"key\":\"value\"}", content);
    }

    [Fact]
    public void WriteText_creates_file()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("/data");
        var (sut, _) = Create(fs);

        sut.WriteText("/data/version.txt", "1.2.0");

        Assert.Equal("1.2.0", fs.File.ReadAllText("/data/version.txt"));
    }

    [Fact]
    public void WriteText_overwrites_existing()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/data/version.txt"] = new("1.0.0"),
        });
        var (sut, _) = Create(fs);

        sut.WriteText("/data/version.txt", "2.0.0");

        Assert.Equal("2.0.0", fs.File.ReadAllText("/data/version.txt"));
    }

    [Fact]
    public void WriteAtomic_produces_complete_file()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("/data/rulesets/local");
        var (sut, _) = Create(fs);

        sut.WriteAtomic("/data/rulesets/local/custom.json", "{\"name\":\"test\"}");

        Assert.Equal("{\"name\":\"test\"}", fs.File.ReadAllText("/data/rulesets/local/custom.json"));
    }

    [Fact]
    public void WriteAtomic_leaves_no_temp_files()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("/data");
        var (sut, _) = Create(fs);

        sut.WriteAtomic("/data/test.json", "content");

        var files = fs.Directory.GetFiles("/data");
        Assert.Single(files);
        Assert.EndsWith("test.json", files[0]);
    }

    [Fact]
    public void Exists_returns_true_for_file()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/data/test.json"] = new("content"),
        });
        var (sut, _) = Create(fs);

        Assert.True(sut.Exists("/data/test.json"));
    }

    [Fact]
    public void Exists_returns_true_for_directory()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("/data/rulesets");
        var (sut, _) = Create(fs);

        Assert.True(sut.Exists("/data/rulesets"));
    }

    [Fact]
    public void Exists_returns_false_for_nonexistent()
    {
        var (sut, _) = Create();

        Assert.False(sut.Exists("/nonexistent"));
    }

    [Fact]
    public void ListFiles_returns_matching_files()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/data/rulesets/a.json"] = new(""),
            ["/data/rulesets/b.json"] = new(""),
            ["/data/rulesets/c.txt"] = new(""),
        });
        var (sut, _) = Create(fs);

        var files = sut.ListFiles("/data/rulesets", "*.json");

        Assert.Equal(2, files.Length);
    }

    [Fact]
    public void ListFiles_returns_empty_for_nonexistent_directory()
    {
        var (sut, _) = Create();

        var files = sut.ListFiles("/nonexistent", "*.json");

        Assert.Empty(files);
    }

    [Fact]
    public void CanWrite_returns_true_for_writable_directory()
    {
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory("/data");
        var (sut, _) = Create(fs);

        Assert.True(sut.CanWrite("/data"));
    }

    [Fact]
    public void CanWrite_returns_false_for_nonexistent_directory()
    {
        var (sut, _) = Create();

        Assert.False(sut.CanWrite("/nonexistent"));
    }

    [Fact]
    public void Watch_requires_real_filesystem()
    {
        // MockFileSystem does not support FileSystemWatcher.
        // Watch() is tested via integration tests.
    }
}
