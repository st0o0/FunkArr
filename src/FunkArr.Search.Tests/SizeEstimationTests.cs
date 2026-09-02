namespace FunkArr.Search.Tests;

public sealed class SizeEstimationTests
{
    [Fact]
    public void EstimateSize_hd_uses_2_5_mbps()
    {
        var size = MediathekViewWebManager.EstimateSize(5400, "hd-url", "sd-url", "low-url");

        Assert.Equal(5400 * 312_500L, size);
    }

    [Fact]
    public void EstimateSize_sd_when_no_hd()
    {
        var size = MediathekViewWebManager.EstimateSize(3600, null, "sd-url", "low-url");

        Assert.Equal(3600 * 187_500L, size);
    }

    [Fact]
    public void EstimateSize_low_when_no_hd_or_sd()
    {
        var size = MediathekViewWebManager.EstimateSize(1800, null, null, "low-url");

        Assert.Equal(1800 * 100_000L, size);
    }

    [Fact]
    public void EstimateSize_zero_when_no_urls()
    {
        var size = MediathekViewWebManager.EstimateSize(5400, null, null, null);

        Assert.Equal(0, size);
    }

    [Fact]
    public void EstimateSize_typical_tatort_90min_hd()
    {
        var size = MediathekViewWebManager.EstimateSize(5400, "hd", "sd", "low");
        var sizeGiB = size / (1024.0 * 1024.0 * 1024.0);

        Assert.InRange(sizeGiB, 1.0, 2.5);
    }
}
