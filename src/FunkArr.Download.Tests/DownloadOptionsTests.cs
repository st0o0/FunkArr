using FunkArr.Core;

namespace FunkArr.Download.Tests;

public sealed class DownloadOptionsTests
{
    [Fact]
    public void Default_path_is_data_downloads()
    {
        var opts = new DownloadOptions();

        Assert.Equal("data/downloads", opts.Path);
    }

    [Fact]
    public void Default_concurrent_downloads_is_three()
    {
        var opts = new DownloadOptions();

        Assert.Equal(3, opts.ConcurrentDownloads);
    }

    [Fact]
    public void Default_categories_is_empty()
    {
        var opts = new DownloadOptions();

        Assert.Empty(opts.Categories);
    }
}
