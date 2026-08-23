using FunkArr.DownloadClient;

namespace FunkArr.Tests.DownloadClient;

public class HlsDownloadServiceTests
{
    [Fact]
    public void BuildFfmpegArgs_ProducesCorrectCommand()
    {
        var args = HlsDownloadService.BuildFfmpegArgs(
            "https://example.com/stream.m3u8",
            "/tmp/output.mp4");

        Assert.Equal(
            "-i \"https://example.com/stream.m3u8\" -map 0:v -map 0:a -c copy -y \"/tmp/output.mp4\"",
            args);
    }

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
