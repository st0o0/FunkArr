using FunkArr.Muxing;

namespace FunkArr.Tests.Muxing;

public class MuxingServiceTests
{
    [Fact]
    public void BuildFfmpegArgs_VideoOnly_NoSubtitle()
    {
        var args = MuxingService.BuildFfmpegArgs("/tmp/video.mp4", null, "/out/video.mkv");

        Assert.Contains("-i \"/tmp/video.mp4\"", args);
        Assert.Contains("-map 0:v -map 0:a", args);
        Assert.Contains("-c copy", args);
        Assert.Contains("language=ger", args);
        Assert.Contains("-y \"/out/video.mkv\"", args);
        Assert.DoesNotContain("-map 1:s", args);
    }

    [Fact]
    public void BuildFfmpegArgs_WithSubtitle_MapsSubtitleStream()
    {
        var args = MuxingService.BuildFfmpegArgs("/tmp/video.mp4", "/tmp/sub.srt", "/out/video.mkv");

        Assert.Contains("-i \"/tmp/video.mp4\"", args);
        Assert.Contains("-i \"/tmp/sub.srt\"", args);
        Assert.Contains("-map 1:s", args);
        Assert.Contains("-c:s srt", args);
        Assert.Contains("-metadata:s:s:0 language=ger", args);
    }
}
