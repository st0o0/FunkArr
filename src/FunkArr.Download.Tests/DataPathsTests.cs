using FunkArr.Core;

namespace FunkArr.Download.Tests;

public sealed class DataPathsTests
{
    private static DataPaths Create(string dataPath = "data", string downloadPath = "data/downloads") =>
        new(new FunkArrOptions { DataPath = dataPath },
            new DownloadOptions { Path = downloadPath });

    [Fact]
    public void DataRoot_is_absolute()
    {
        var paths = Create();

        Assert.True(Path.IsPathRooted(paths.DataRoot));
    }

    [Fact]
    public void Database_is_under_data_root()
    {
        var paths = Create();

        Assert.Equal(Path.Join(paths.DataRoot, "funkarr.db"), paths.Database);
    }

    [Fact]
    public void CommunityRuleSets_follows_convention()
    {
        var paths = Create();

        Assert.Equal(Path.Join(paths.DataRoot, "rulesets", "community"), paths.CommunityRuleSets);
    }

    [Fact]
    public void LocalRuleSets_follows_convention()
    {
        var paths = Create();

        Assert.Equal(Path.Join(paths.DataRoot, "rulesets", "local"), paths.LocalRuleSets);
    }

    [Fact]
    public void RuleSetVersion_follows_convention()
    {
        var paths = Create();

        Assert.Equal(Path.Join(paths.DataRoot, "rulesets", "version.txt"), paths.RuleSetVersion);
    }

    [Fact]
    public void Temp_follows_convention()
    {
        var paths = Create();

        Assert.Equal(Path.Join(paths.DataRoot, "temp"), paths.Temp);
    }

    [Fact]
    public void Custom_data_path_resolves_correctly()
    {
        var paths = Create(dataPath: "/app/data");

        Assert.Equal(Path.GetFullPath("/app/data"), paths.DataRoot);
        Assert.Equal(Path.Join(paths.DataRoot, "funkarr.db"), paths.Database);
        Assert.Equal(Path.Join(paths.DataRoot, "rulesets", "community"), paths.CommunityRuleSets);
    }

    [Fact]
    public void DownloadRoot_is_absolute()
    {
        var paths = Create();

        Assert.True(Path.IsPathRooted(paths.DownloadRoot));
    }

    [Fact]
    public void Incomplete_follows_convention()
    {
        var paths = Create();

        Assert.Equal(Path.Join(paths.DownloadRoot, "incomplete"), paths.Incomplete);
    }

    [Fact]
    public void Complete_follows_convention()
    {
        var paths = Create();

        Assert.Equal(Path.Join(paths.DownloadRoot, "complete"), paths.Complete);
    }

    [Fact]
    public void Custom_download_path_resolves_correctly()
    {
        var paths = Create(downloadPath: "/shared/downloads");

        Assert.Equal(Path.GetFullPath("/shared/downloads"), paths.DownloadRoot);
        Assert.Equal(Path.Join(paths.DownloadRoot, "incomplete"), paths.Incomplete);
        Assert.Equal(Path.Join(paths.DownloadRoot, "complete"), paths.Complete);
    }

    [Fact]
    public void ResolveDownload_with_episode_identifier_uses_title_as_dir()
    {
        var paths = Create(downloadPath: "/downloads");
        var categories = new List<DownloadCategory> { new() { Name = "tv", Dir = "tv" } };

        var result = paths.ResolveDownload("abc12345-0000-0000-0000-000000000000", "Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr", "tv", categories);

        Assert.Equal(Path.Join(paths.Incomplete, "abc12345-0000-0000-0000-000000000000", "Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"), result.IncompletePath);
        Assert.Equal(Path.Join(paths.Complete, "tv", "Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr", "Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"), result.CompletePath);
        Assert.Equal(Path.Join("tv", "Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr", "Show.S01E05.Title.GERMAN.720p.WEB.h264-FunkArr.mkv"), result.RelativePath);
    }

    [Fact]
    public void ResolveDownload_without_episode_identifier_adds_id_prefix()
    {
        var paths = Create(downloadPath: "/downloads");
        var categories = new List<DownloadCategory> { new() { Name = "tv", Dir = "tv" } };

        var result = paths.ResolveDownload("a1b2c3d4-e5f6-7890-abcd-000000000000", "Show.Title.GERMAN.720p.WEB.h264-FunkArr", "tv", categories);

        Assert.Contains("Show.Title.GERMAN.720p.WEB.h264-FunkArr-a1b2c3d4", result.CompletePath);
        Assert.Contains("Show.Title.GERMAN.720p.WEB.h264-FunkArr.mkv", result.CompletePath);
    }

    [Fact]
    public void ResolveDownload_with_date_identifier_uses_title_as_dir()
    {
        var paths = Create(downloadPath: "/downloads");
        var categories = new List<DownloadCategory>();

        var result = paths.ResolveDownload("abc12345", "Show.2026-09-03.Title.GERMAN.720p.WEB.h264-FunkArr", null, categories);

        Assert.DoesNotContain("-abc12345", result.CompletePath);
    }

    [Fact]
    public void ResolveDownload_with_unknown_category_omits_category_dir()
    {
        var paths = Create(downloadPath: "/downloads");
        var categories = new List<DownloadCategory> { new() { Name = "tv", Dir = "tv" } };

        var result = paths.ResolveDownload("abc12345", "Show.S01E05", "unknown", categories);

        Assert.DoesNotContain("unknown", result.RelativePath);
        Assert.Equal(Path.Join("Show.S01E05", "Show.S01E05.mkv"), result.RelativePath);
    }

    [Fact]
    public void ResolveDownload_with_null_category_omits_category_dir()
    {
        var paths = Create(downloadPath: "/downloads");
        var categories = new List<DownloadCategory> { new() { Name = "tv", Dir = "tv" } };

        var result = paths.ResolveDownload("abc12345", "Show.S01E05", null, categories);

        Assert.Equal(Path.Join("Show.S01E05", "Show.S01E05.mkv"), result.RelativePath);
    }

    [Fact]
    public void ResolveDownload_with_custom_category_dir()
    {
        var paths = Create(downloadPath: "/downloads");
        var categories = new List<DownloadCategory> { new() { Name = "dokus", Dir = "dokumentationen" } };

        var result = paths.ResolveDownload("abc12345", "Show.S01E05", "dokus", categories);

        Assert.StartsWith(Path.Join("dokumentationen", "Show.S01E05"), result.RelativePath);
    }

    [Theory]
    [InlineData("Show.S01E05.Title", true)]
    [InlineData("Show.S100E200.Title", true)]
    [InlineData("Show.E05.Title", true)]
    [InlineData("Show.2026-09-03.Title", true)]
    [InlineData("Show.Title.GERMAN.720p", false)]
    [InlineData("Show.S1E1.Title", false)]
    public void HasEpisodeIdentifier_detects_patterns(string title, bool expected)
    {
        Assert.Equal(expected, DataPaths.HasEpisodeIdentifier(title));
    }

    [Fact]
    public void ResolveDownload_category_matching_is_case_insensitive()
    {
        var paths = Create(downloadPath: "/downloads");
        var categories = new List<DownloadCategory> { new() { Name = "TV", Dir = "tv" } };

        var result = paths.ResolveDownload("abc12345", "Show.S01E05", "tv", categories);

        Assert.StartsWith(Path.Join("tv", "Show.S01E05"), result.RelativePath);
    }

    [Fact]
    public void ResolveDownload_category_with_empty_dir_uses_name()
    {
        var paths = Create(downloadPath: "/downloads");
        var categories = new List<DownloadCategory> { new() { Name = "sonarr", Dir = "" } };

        var result = paths.ResolveDownload("abc12345", "Show.S01E05", "sonarr", categories);

        Assert.StartsWith(Path.Join("sonarr", "Show.S01E05"), result.RelativePath);
    }
}
