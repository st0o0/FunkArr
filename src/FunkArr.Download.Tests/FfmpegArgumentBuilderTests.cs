using FunkArr.Download;

namespace FunkArr.Download.Tests;

public sealed class FfmpegArgumentBuilderTests
{
    [Fact]
    public void Build_direct_http_without_subtitle()
    {
        var args = FfmpegArgumentBuilder.Build(
            "https://example.com/video.mp4", null, "/downloads/output.mkv");

        Assert.Equal(
            "-y -i \"https://example.com/video.mp4\" -c copy -progress pipe:1 \"/downloads/output.mkv\"",
            args);
    }

    [Fact]
    public void Build_direct_http_with_subtitle()
    {
        var args = FfmpegArgumentBuilder.Build(
            "https://example.com/video.mp4",
            "https://example.com/subs.ttml",
            "/downloads/output.mkv");

        Assert.Equal(
            "-y -i \"https://example.com/video.mp4\" -i \"https://example.com/subs.ttml\" "
            + "-c:v copy -c:a copy -c:s srt -metadata:s:s:0 language=deu "
            + "-progress pipe:1 \"/downloads/output.mkv\"",
            args);
    }

    [Fact]
    public void Build_hls_without_subtitle()
    {
        var args = FfmpegArgumentBuilder.Build(
            "https://apasfiis.sf.apa.at/ipad/cms-austria/chunklist.m3u8",
            null,
            "/downloads/output.mkv");

        Assert.Contains("chunklist.m3u8", args);
        Assert.Contains("-c copy", args);
        Assert.Contains("-y", args);
    }

    [Fact]
    public void Build_hls_with_subtitle()
    {
        var args = FfmpegArgumentBuilder.Build(
            "https://apasfiis.sf.apa.at/ipad/chunklist.m3u8",
            "https://api-tvthek.orf.at/assets/subtitles/subs.ttml",
            "/downloads/output.mkv");

        Assert.Contains("-c:s srt", args);
        Assert.Contains("language=deu", args);
        Assert.Contains("chunklist.m3u8", args);
    }

    [Fact]
    public void Build_always_includes_overwrite_flag()
    {
        var args = FfmpegArgumentBuilder.Build("https://example.com/v.mp4", null, "/out.mkv");

        Assert.StartsWith("-y", args);
    }

    [Fact]
    public void BuildWithoutSubtitle_is_shorthand()
    {
        var full = FfmpegArgumentBuilder.Build("https://example.com/v.mp4", null, "/out.mkv");
        var shorthand = FfmpegArgumentBuilder.BuildWithoutSubtitle("https://example.com/v.mp4", "/out.mkv");

        Assert.Equal(full, shorthand);
    }
}
