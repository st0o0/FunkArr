using System.IO.Abstractions;
using FunkArr.Core;
using FunkArr.RuleSet.DiskModel;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly RuleSetStore _store;

    public RuleSetStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        var funkArrOptions = new FunkArrOptions { DataPath = _tempDir };
        var downloadOptions = new DownloadOptions();
        var dataPaths = new DataPaths(funkArrOptions, downloadOptions);
        dataPaths.EnsureDirectories();
        Directory.CreateDirectory(Path.Combine(_tempDir, "rulesets", "community"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "rulesets", "local"));
        var dataFiles = new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance);
        _store = new RuleSetStore(dataFiles, dataPaths);
    }

    private static readonly string SampleJson = """
        {"topic":"Test","media":{"name":"Test","type":"show"},"rules":[{"id":"test-rule","strategy":"itemTitleIncludes"}]}
        """;

    [Fact]
    public void Load_returns_null_for_nonexistent()
    {
        var (community, local) = _store.Load("nonexistent");
        Assert.Null(community);
        Assert.Null(local);
    }

    [Fact]
    public void Load_returns_community_when_exists()
    {
        File.WriteAllText(Path.Combine(_tempDir, "rulesets", "community", "test-show.json"), SampleJson);
        var (community, local) = _store.Load("test-show");
        Assert.NotNull(community);
        Assert.Null(local);
        Assert.Equal("Test", community!.Topic);
    }

    [Fact]
    public void LoadMerged_merges_community_and_local()
    {
        File.WriteAllText(Path.Combine(_tempDir, "rulesets", "community", "merged-show.json"), SampleJson);
        File.WriteAllText(Path.Combine(_tempDir, "rulesets", "local", "merged-show.json"),
            """{"topic":"Local Override","media":{"name":"Test","type":"show"},"rules":[{"id":"local-rule","strategy":"itemTitleExact"}]}""");

        var merged = _store.LoadMerged("merged-show");
        Assert.NotNull(merged);
        Assert.Equal("Test", merged!.Topic);
        Assert.Equal(2, merged.Rules!.Count);
    }

    [Fact]
    public void SaveLocal_writes_and_returns_json()
    {
        var disk = new DiskRuleSet
        {
            Topic = "Saved",
            Media = new DiskMedia { Name = "Saved", Type = Messages.MediaType.Show },
            Rules = [new DiskRule { Id = "save-rule", Strategy = "itemTitleIncludes" }],
        };

        var json = _store.SaveLocal("saved-show", disk);
        Assert.Contains("Saved", json);
        Assert.True(_store.ExistsLocal("saved-show"));
    }

    [Fact]
    public void DeleteLocal_removes_file()
    {
        var disk = new DiskRuleSet { Topic = "Delete Me", Rules = [] };
        _store.SaveLocal("delete-me", disk);
        Assert.True(_store.ExistsLocal("delete-me"));

        Assert.True(_store.DeleteLocal("delete-me"));
        Assert.False(_store.ExistsLocal("delete-me"));
    }

    [Fact]
    public void DeleteLocal_returns_false_for_nonexistent()
    {
        Assert.False(_store.DeleteLocal("nonexistent"));
    }

    [Fact]
    public void Scan_discovers_community_and_local_files()
    {
        File.WriteAllText(Path.Combine(_tempDir, "rulesets", "community", "show-a.json"), SampleJson);
        File.WriteAllText(Path.Combine(_tempDir, "rulesets", "community", "show-b.json"), SampleJson);
        File.WriteAllText(Path.Combine(_tempDir, "rulesets", "local", "show-b.json"), SampleJson);
        File.WriteAllText(Path.Combine(_tempDir, "rulesets", "local", "show-c.json"), SampleJson);

        var result = _store.Scan();
        Assert.Equal(3, result.Count);
        Assert.NotNull(result["show-a"].CommunityPath);
        Assert.Null(result["show-a"].LocalPath);
        Assert.NotNull(result["show-b"].CommunityPath);
        Assert.NotNull(result["show-b"].LocalPath);
        Assert.Null(result["show-c"].CommunityPath);
        Assert.NotNull(result["show-c"].LocalPath);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
