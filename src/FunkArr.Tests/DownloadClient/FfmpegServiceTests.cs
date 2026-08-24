using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Ffmpeg;

namespace FunkArr.Tests.DownloadClient;

public class FfmpegServiceTests
{
    [Fact]
    public void BuildHlsDownloadArgs_ProducesCorrectArguments()
    {
        var result = FfmpegService.BuildHlsDownloadArgs("https://example.com/stream.m3u8", "/tmp/abc.mp4");

        Assert.Equal("-i \"https://example.com/stream.m3u8\" -map 0:v -map 0:a -c copy -y \"/tmp/abc.mp4\"", result);
    }

    [Fact]
    public void BuildRemuxArgs_WithSubtitle_IncludesSubtitleMapping()
    {
        var result = FfmpegService.BuildRemuxArgs("/tmp/video.mp4", "/tmp/sub.srt", "/out/Show/Show.mkv");

        Assert.Contains("-i \"/tmp/video.mp4\" -i \"/tmp/sub.srt\"", result);
        Assert.Contains("-map 0:v -map 0:a -map 1:s", result);
        Assert.Contains("-c copy -c:s srt", result);
        Assert.Contains("-metadata:s:s:0 language=ger", result);
        Assert.Contains("-y \"/out/Show/Show.mkv\"", result);
    }

    [Fact]
    public void BuildRemuxArgs_WithoutSubtitle_OmitsSubtitleMapping()
    {
        var result = FfmpegService.BuildRemuxArgs("/tmp/video.mp4", null, "/out/Show/Show.mkv");

        Assert.Contains("-i \"/tmp/video.mp4\"", result);
        Assert.Contains("-map 0:v -map 0:a", result);
        Assert.DoesNotContain("-map 1:s", result);
        Assert.DoesNotContain("-c:s srt", result);
        Assert.Contains("-metadata:s:v:0 language=ger", result);
        Assert.Contains("-y \"/out/Show/Show.mkv\"", result);
    }
}
