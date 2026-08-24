using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Tracker;

namespace FunkArr.Tests.DownloadClient;

public class DownloadSourceDetectorTests
{
    [Fact]
    public void DownloadSourceDetector_Mp4Url_ReturnsDirect()
    {
        var result = DownloadSourceDetector.Detect("https://example.com/video.mp4");

        Assert.Equal(DownloadSourceType.Direct, result);
    }

    [Fact]
    public void DownloadSourceDetector_M3u8Url_ReturnsHls()
    {
        var result = DownloadSourceDetector.Detect("https://example.com/stream.m3u8");

        Assert.Equal(DownloadSourceType.Hls, result);
    }

    [Fact]
    public void DownloadSourceDetector_M3u8WithQueryString_ReturnsHls()
    {
        var result = DownloadSourceDetector.Detect("https://example.com/stream.m3u8?token=abc");

        Assert.Equal(DownloadSourceType.Hls, result);
    }

    [Fact]
    public void DownloadSourceDetector_UnknownExtension_ReturnsDirect()
    {
        var result = DownloadSourceDetector.Detect("https://example.com/video");

        Assert.Equal(DownloadSourceType.Direct, result);
    }
}
