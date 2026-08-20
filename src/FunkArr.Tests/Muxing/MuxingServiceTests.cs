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

    [Fact]
    public void ConvertVttToSrt_RemovesWebVttHeader()
    {
        var vtt = "WEBVTT\n\n00:00:01.000 --> 00:00:04.000\nHello World\n\n";
        var srt = MuxingService.ConvertVttToSrt(vtt);

        Assert.DoesNotContain("WEBVTT", srt);
        Assert.Contains("1", srt);
        Assert.Contains("00:00:01,000 --> 00:00:04,000", srt);
        Assert.Contains("Hello World", srt);
    }

    [Fact]
    public void ConvertVttToSrt_ReplacesDotsWithCommas()
    {
        var vtt = "WEBVTT\n\n00:00:01.500 --> 00:00:04.200\nTest\n\n";
        var srt = MuxingService.ConvertVttToSrt(vtt);

        Assert.Contains("00:00:01,500 --> 00:00:04,200", srt);
    }

    [Fact]
    public void ConvertTtmlToSrt_ExtractsTimedParagraphs()
    {
        var ttml = "<tt><body><div><p begin=\"00:00:01.000\" end=\"00:00:04.000\">Hello</p></div></body></tt>";
        var srt = MuxingService.ConvertTtmlToSrt(ttml);

        Assert.Contains("1", srt);
        Assert.Contains("00:00:01,000 --> 00:00:04,000", srt);
        Assert.Contains("Hello", srt);
    }

    [Fact]
    public void ConvertTtmlToSrt_StripsInlineTags()
    {
        var ttml = "<tt><body><div><p begin=\"0:00:01.000\" end=\"0:00:02.000\"><span>Bold</span> text</p></div></body></tt>";
        var srt = MuxingService.ConvertTtmlToSrt(ttml);

        Assert.Contains("Bold text", srt);
        Assert.DoesNotContain("<span>", srt);
    }

    [Fact]
    public void NormalizeTtmlTimestamp_ReplacesDotsWithCommas()
    {
        Assert.Equal("00:00:01,500", MuxingService.NormalizeTtmlTimestamp("00:00:01.500"));
    }

    [Fact]
    public void NormalizeTtmlTimestamp_AddsMilliseconds()
    {
        Assert.Equal("00:00:01,000", MuxingService.NormalizeTtmlTimestamp("00:00:01"));
    }
}
