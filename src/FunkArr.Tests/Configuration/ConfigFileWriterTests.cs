using System.Text.Json.Nodes;
using FunkArr.Configuration;

namespace FunkArr.Tests.Configuration;

public sealed class ConfigFileWriterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ConfigFileWriter _sut;

    public ConfigFileWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "funkarr-cfw-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var options = new FunkArrOptions
        {
            PersistencePath = Path.Combine(_tempDir, "funkarr.db"),
        };
        _sut = new ConfigFileWriter(options);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Write_CreatesFile_WhenNotExists()
    {
        var partial = new JsonObject { ["apiKey"] = "abc123" };

        _sut.Write(partial);

        var configPath = Path.Combine(_tempDir, "config.json");
        Assert.True(File.Exists(configPath));
        var content = File.ReadAllText(configPath);
        Assert.Contains("abc123", content);
    }

    [Fact]
    public void Write_MergesWithExistingContent()
    {
        _sut.Write(new JsonObject { ["apiKey"] = "abc123" });
        _sut.Write(new JsonObject { ["downloadPath"] = "/downloads" });

        var result = _sut.Read();
        Assert.Equal("abc123", result["apiKey"]?.GetValue<string>());
        Assert.Equal("/downloads", result["downloadPath"]?.GetValue<string>());
    }

    [Fact]
    public void Write_DeepMergesNestedObjects()
    {
        _sut.Write(new JsonObject
        {
            ["nested"] = new JsonObject
            {
                ["a"] = 1,
                ["b"] = 2,
            },
        });

        _sut.Write(new JsonObject
        {
            ["nested"] = new JsonObject
            {
                ["c"] = 3,
            },
        });

        var result = _sut.Read();
        var nested = result["nested"]!.AsObject();
        Assert.Equal(1, nested["a"]?.GetValue<int>());
        Assert.Equal(2, nested["b"]?.GetValue<int>());
        Assert.Equal(3, nested["c"]?.GetValue<int>());
    }

    [Fact]
    public void Write_OverwritesExistingKeys()
    {
        _sut.Write(new JsonObject { ["apiKey"] = "old" });
        _sut.Write(new JsonObject { ["apiKey"] = "new" });

        var result = _sut.Read();
        Assert.Equal("new", result["apiKey"]?.GetValue<string>());
    }

    [Fact]
    public void Read_ReturnsEmptyObject_WhenFileDoesNotExist()
    {
        var result = _sut.Read();
        Assert.Empty(result);
    }
}
