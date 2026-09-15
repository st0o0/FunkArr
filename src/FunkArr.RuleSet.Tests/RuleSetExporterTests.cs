using System.IO.Abstractions;
using System.Text.Json;
using FunkArr.Core;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetExporterTests
{
    private const string ValidLocalJson = """
        {
          "topic": "My Local Show",
          "aliases": [],
          "media": { "name": "My Local Show", "type": "show" },
          "confidence": 0.9,
          "rules": [
            {
              "id": "airdate",
              "priority": 0,
              "strategy": "itemTitleEqualsAirdate"
            }
          ]
        }
        """;

    private const string CommunityJson = """
        {
          "topic": "Shared Show",
          "aliases": ["Alias One"],
          "media": { "name": "Shared Show", "type": "show", "imdbId": "tt0000001" },
          "confidence": 1.0,
          "rules": [
            {
              "id": "community-airdate",
              "priority": 0,
              "strategy": "itemTitleEqualsAirdate"
            },
            {
              "id": "community-title",
              "priority": 1,
              "strategy": "itemTitleExact",
              "titleRules": [{ "type": "static", "value": "Shared Show" }]
            }
          ]
        }
        """;

    private const string LocalOverrideJson = """
        {
          "topic": "Shared Show",
          "rules": [
            {
              "id": "community-airdate",
              "priority": 0,
              "confidence": 0.5,
              "strategy": "itemTitleEqualsAirdate"
            },
            {
              "id": "local-extra",
              "priority": 2,
              "strategy": "itemTitleEqualsAirdate"
            }
          ]
        }
        """;

    private const string StandaloneLocalJson = """
        {
          "topic": "Standalone Show",
          "standalone": true,
          "media": { "name": "Standalone Show", "type": "show" },
          "confidence": 0.7,
          "rules": [
            {
              "id": "local-only",
              "priority": 0,
              "strategy": "itemTitleEqualsAirdate"
            }
          ]
        }
        """;

    private const string LocalWithDisableJson = """
        {
          "topic": "Shared Show",
          "disable": ["community-title"],
          "rules": []
        }
        """;

    private static (RuleSetExporter exporter, StubDataFiles dataFiles) CreateExporter(
        string? communityJson = null, string? localJson = null, string ruleSetId = "test-show")
    {
        var dataFiles = new StubDataFiles();
        var dataPaths = new DataPaths(new FunkArrOptions { DataPath = "/data" }, new DownloadOptions());

        if (communityJson is not null)
        {
            dataFiles.AddFile(Path.Join(dataPaths.CommunityRuleSets, $"{ruleSetId}.json"), communityJson);
        }

        if (localJson is not null)
        {
            dataFiles.AddFile(Path.Join(dataPaths.LocalRuleSets, $"{ruleSetId}.json"), localJson);
        }

        var validator = new RuleSetValidator();
        var exporter = new RuleSetExporter(dataFiles, dataPaths, validator);
        return (exporter, dataFiles);
    }

    [Fact]
    public void Export_standalone_local_returns_json()
    {
        var (exporter, _) = CreateExporter(localJson: ValidLocalJson);

        var result = exporter.Export("test-show");

        Assert.True(result.Success);
        Assert.NotNull(result.Json);

        var doc = JsonDocument.Parse(result.Json);
        Assert.Equal("My Local Show", doc.RootElement.GetProperty("topic").GetString());
    }

    [Fact]
    public void Export_merged_contains_all_rules()
    {
        var (exporter, _) = CreateExporter(communityJson: CommunityJson, localJson: LocalOverrideJson);

        var result = exporter.Export("test-show");

        Assert.True(result.Success);
        Assert.NotNull(result.Json);

        var doc = JsonDocument.Parse(result.Json);
        var rules = doc.RootElement.GetProperty("rules");
        Assert.Equal(3, rules.GetArrayLength());
    }

    [Fact]
    public void Export_standalone_override_strips_standalone_field()
    {
        var (exporter, _) = CreateExporter(communityJson: CommunityJson, localJson: StandaloneLocalJson);

        var result = exporter.Export("test-show");

        Assert.True(result.Success);
        Assert.NotNull(result.Json);

        var doc = JsonDocument.Parse(result.Json);
        Assert.False(doc.RootElement.TryGetProperty("standalone", out _));
    }

    [Fact]
    public void Export_strips_disable_field()
    {
        var (exporter, _) = CreateExporter(communityJson: CommunityJson, localJson: LocalWithDisableJson);

        var result = exporter.Export("test-show");

        Assert.True(result.Success);
        Assert.NotNull(result.Json);

        var doc = JsonDocument.Parse(result.Json);
        Assert.False(doc.RootElement.TryGetProperty("disable", out _));
    }

    [Fact]
    public void Export_community_only_returns_error()
    {
        var (exporter, _) = CreateExporter(communityJson: CommunityJson);

        var result = exporter.Export("test-show");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("local", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Export_unknown_returns_error()
    {
        var (exporter, _) = CreateExporter();

        var result = exporter.Export("nonexistent");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    private sealed class StubDataFiles : IDataFiles
    {
        private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

        public void AddFile(string path, string content) => _files[path] = content;

        public bool Exists(string path) => _files.ContainsKey(path);
        public string ReadText(string path) => _files[path];

        public void CreateDirectory(string path) { }
        public void Remove(string path) => _files.Remove(path);
        public void Move(string source, string destination) { }
        public void ReplaceDirectory(string source, string target) { }
        public void WriteText(string path, string content) => _files[path] = content;
        public void WriteAtomic(string path, string content) => _files[path] = content;
        public string[] ListFiles(string directory, string pattern) => [];
        public bool CanWrite(string directory) => true;
        public IFileSystemWatcher Watch(string directory, string filter) =>
            throw new NotSupportedException();
    }
}
