using FunkArr.Core;

namespace FunkArr.Download.Tests;

public sealed class DownloadOptionsTests
{
    [Fact]
    public void ResolveCategoryDir_known_category_returns_name()
    {
        var opts = new DownloadOptions
        {
            Categories = [new DownloadCategory { Name = "sonarr" }],
        };

        Assert.Equal("sonarr", opts.ResolveCategoryDir("sonarr"));
    }

    [Fact]
    public void ResolveCategoryDir_known_category_with_custom_dir()
    {
        var opts = new DownloadOptions
        {
            Categories = [new DownloadCategory { Name = "dokus", Dir = "dokumentationen" }],
        };

        Assert.Equal("dokumentationen", opts.ResolveCategoryDir("dokus"));
    }

    [Fact]
    public void ResolveCategoryDir_unknown_category_returns_empty()
    {
        var opts = new DownloadOptions
        {
            Categories = [new DownloadCategory { Name = "sonarr" }],
        };

        Assert.Equal("", opts.ResolveCategoryDir("unknown"));
    }

    [Fact]
    public void ResolveCategoryDir_empty_category_returns_empty()
    {
        var opts = new DownloadOptions
        {
            Categories = [new DownloadCategory { Name = "sonarr" }],
        };

        Assert.Equal("", opts.ResolveCategoryDir(""));
    }

    [Fact]
    public void ResolveCategoryDir_null_category_returns_empty()
    {
        var opts = new DownloadOptions
        {
            Categories = [new DownloadCategory { Name = "sonarr" }],
        };

        Assert.Equal("", opts.ResolveCategoryDir(null));
    }

    [Fact]
    public void ResolveCategoryDir_case_insensitive()
    {
        var opts = new DownloadOptions
        {
            Categories = [new DownloadCategory { Name = "sonarr" }],
        };

        Assert.Equal("sonarr", opts.ResolveCategoryDir("Sonarr"));
    }

    [Fact]
    public void CompletePath_derives_from_download_path()
    {
        var opts = new DownloadOptions { DownloadPath = "/downloads" };

        Assert.Equal(Path.Combine("/downloads", "complete"), opts.CompletePath);
    }

    [Fact]
    public void IncompletePath_derives_from_download_path()
    {
        var opts = new DownloadOptions { DownloadPath = "/downloads" };

        Assert.Equal(Path.Combine("/downloads", "incomplete"), opts.IncompletePath);
    }

    [Fact]
    public void Default_categories_is_empty()
    {
        var opts = new DownloadOptions();

        Assert.Empty(opts.Categories);
    }
}
