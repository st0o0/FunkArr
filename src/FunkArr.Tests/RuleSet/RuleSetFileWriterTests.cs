using System.Text.Json;
using FunkArr.RuleSet;

namespace FunkArr.Tests.RuleSet;

public sealed class RuleSetFileWriterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "funkarr-writer-test-" + Guid.NewGuid().ToString("N")[..8]);

    public RuleSetFileWriterTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Write_CreatesJsonFileWithSlugifiedName()
    {
        var ruleSet = CreateRuleSet("Tatort");

        RuleSetFileWriter.Write(_tempDir, ruleSet);

        var files = Directory.GetFiles(_tempDir, "*.json");
        Assert.Single(files);
        Assert.Equal("tatort.json", Path.GetFileName(files[0]));
    }

    [Fact]
    public void Write_FileContentRoundTrips()
    {
        var ruleSet = CreateRuleSet("Heute Show");

        RuleSetFileWriter.Write(_tempDir, ruleSet);

        var files = Directory.GetFiles(_tempDir, "*.json");
        var json = File.ReadAllText(files[0]);
        var deserialized = JsonSerializer.Deserialize<RuleSetFile>(json, RuleSetJsonOptions.Default);

        Assert.NotNull(deserialized);
        Assert.Equal("Heute Show", deserialized.Topic);
        Assert.Equal("tvdb", deserialized.Source);
        Assert.Single(deserialized.Rules);
        Assert.Equal(MatchingStrategy.SeasonAndEpisodeNumber, deserialized.Rules[0].Strategy);
    }

    [Fact]
    public void Write_CreatesDirectoryIfNotExists()
    {
        var subDir = Path.Combine(_tempDir, "nested", "dir");
        var ruleSet = CreateRuleSet("Test");

        RuleSetFileWriter.Write(subDir, ruleSet);

        Assert.True(Directory.Exists(subDir));
        Assert.Single(Directory.GetFiles(subDir, "*.json"));
    }

    private static RuleSetFile CreateRuleSet(string topic) => new()
    {
        Topic = topic,
        Media = new MediaReference { Name = topic, TvdbId = 12345 },
        Source = "tvdb",
        Rules =
        [
            new Rule { Strategy = MatchingStrategy.SeasonAndEpisodeNumber },
        ],
    };
}
