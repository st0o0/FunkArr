using FunkArr.Core.Configuration;
using FunkArr.Core.Data;
using Microsoft.Extensions.Options;

namespace FunkArr.Download.Tests;

public sealed class ResolveDownloadSanitizationTests
{
    private static DataPaths Create(string downloadPath = "/downloads") =>
        new(Options.Create(new FunkArrOptions { DataPath = "/app/data" }),
            Options.Create(new DownloadOptions { Path = downloadPath }));

    private static readonly List<DownloadCategory> _categories =
        [new() { Name = "tv", Dir = "tv" }];

    [Fact]
    public void Normal_title_resolves_correctly()
    {
        var paths = Create();

        var result = paths.ResolveDownload("abc12345-0000", "Show.S01E05.GERMAN.1080p.WEB.h264-FunkArr", "tv", _categories);

        Assert.StartsWith(paths.Incomplete, result.IncompletePath);
        Assert.StartsWith(paths.Complete, result.CompletePath);
    }

    [Fact]
    public void Title_with_path_traversal_stays_confined()
    {
        var paths = Create();

        var result = paths.ResolveDownload("abc12345-0000", "../../etc/passwd", "tv", _categories);

        Assert.StartsWith(paths.Incomplete, Path.GetFullPath(result.IncompletePath));
        Assert.StartsWith(paths.Complete, Path.GetFullPath(result.CompletePath));
    }

    [Fact]
    public void Title_with_forward_slashes_stays_confined()
    {
        var paths = Create();

        var result = paths.ResolveDownload("abc12345-0000", "Show/Season/Episode", "tv", _categories);

        Assert.StartsWith(paths.Incomplete, Path.GetFullPath(result.IncompletePath));
        Assert.StartsWith(paths.Complete, Path.GetFullPath(result.CompletePath));
        Assert.DoesNotContain("/Season/", result.CompletePath);
    }

    [Fact]
    public void Title_with_backslashes_stays_confined()
    {
        var paths = Create();

        var result = paths.ResolveDownload("abc12345-0000", "Show\\Subtitle", "tv", _categories);

        Assert.StartsWith(paths.Complete, Path.GetFullPath(result.CompletePath));
    }

    [Fact]
    public void Empty_title_uses_unnamed_fallback()
    {
        var paths = Create();

        var result = paths.ResolveDownload("abc12345-0000", "", "tv", _categories);

        Assert.Contains("unnamed", result.CompletePath);
    }

    [Fact]
    public void All_invalid_title_uses_unnamed_fallback()
    {
        var paths = Create();

        var result = paths.ResolveDownload("abc12345-0000", "///...", "tv", _categories);

        Assert.Contains("unnamed", result.CompletePath);
    }

    [Fact]
    public void Confinement_check_validates_incomplete_path()
    {
        var paths = Create();

        var result = paths.ResolveDownload("abc12345-0000", "Show.S01E05", null, []);

        var fullIncomplete = Path.GetFullPath(result.IncompletePath);
        Assert.StartsWith(paths.Incomplete, fullIncomplete);
    }

    [Fact]
    public void Confinement_check_validates_complete_path()
    {
        var paths = Create();

        var result = paths.ResolveDownload("abc12345-0000", "Show.S01E05", null, []);

        var fullComplete = Path.GetFullPath(result.CompletePath);
        Assert.StartsWith(paths.Complete, fullComplete);
    }
}
