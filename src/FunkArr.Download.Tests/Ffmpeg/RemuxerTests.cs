using FFmpeg.Rx;
using FunkArr.Download.Ffmpeg;

namespace FunkArr.Download.Tests.Ffmpeg;

public sealed class RemuxerTests
{
    [Fact]
    public async Task BuildSession_direct_mp4_creates_session()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);

        await using var session = FfmpegProcess.BuildSession(input);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task BuildSession_hls_creates_session()
    {
        var input = new FfmpegInput("https://example.com/stream.m3u8", null, "/tmp/out.mkv", null, null, true);

        await using var session = FfmpegProcess.BuildSession(input);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task BuildSession_with_subtitle_creates_session()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", "/tmp/subtitle.srt", "/tmp/out.mkv", null, null, false);

        await using var session = FfmpegProcess.BuildSession(input);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task BuildSession_with_proxy_creates_session()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", "http://proxy:8888", null, false);

        await using var session = FfmpegProcess.BuildSession(input);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task BuildSession_hls_with_subtitle_creates_session()
    {
        var input = new FfmpegInput("https://example.com/stream.m3u8", "/tmp/subtitle.srt", "/tmp/out.mkv", null, "deu", true);

        await using var session = FfmpegProcess.BuildSession(input);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task BuildSession_progress_observable_available()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);

        await using var session = FfmpegProcess.BuildSession(input);

        Assert.NotNull(session.Progress);
    }

    [Fact]
    public void RemuxOptions_IsHls_true_for_m3u8()
    {
        var options = RemuxOptions.Create("https://example.com/stream.m3u8", "/tmp/out.mkv");
        Assert.True(options.IsHls);
    }

    [Fact]
    public void RemuxOptions_IsHls_true_for_uppercase_M3U8()
    {
        var options = RemuxOptions.Create("https://example.com/stream.M3U8", "/tmp/out.mkv");
        Assert.True(options.IsHls);
    }

    [Fact]
    public void RemuxOptions_IsHls_false_for_mp4()
    {
        var options = RemuxOptions.Create("https://example.com/video.mp4", "/tmp/out.mkv");
        Assert.False(options.IsHls);
    }

    [Fact]
    public void RemuxOptions_Create_sets_required_fields()
    {
        var options = RemuxOptions.Create("https://example.com/video.mp4", "/tmp/out.mkv");

        Assert.Equal("https://example.com/video.mp4", options.VideoUrl);
        Assert.Equal("/tmp/out.mkv", options.OutputPath);
        Assert.Null(options.SubtitleUrl);
        Assert.Null(options.Channel);
    }

    [Fact]
    public void RemuxOptions_WithSubtitle_returns_new_instance()
    {
        var original = RemuxOptions.Create("https://example.com/video.mp4", "/tmp/out.mkv");
        var withSub = original.WithSubtitle("https://example.com/subs.ttml");

        Assert.Null(original.SubtitleUrl);
        Assert.Equal("https://example.com/subs.ttml", withSub.SubtitleUrl);
    }

    [Fact]
    public void RemuxOptions_WithChannel_returns_new_instance()
    {
        var original = RemuxOptions.Create("https://example.com/video.mp4", "/tmp/out.mkv");
        var withChannel = original.WithChannel("SRF");

        Assert.Null(original.Channel);
        Assert.Equal("SRF", withChannel.Channel);
    }

    [Fact]
    public void RemuxOptions_fluent_chaining()
    {
        var options = RemuxOptions.Create("https://example.com/video.mp4", "/tmp/out.mkv")
            .WithSubtitle("https://example.com/subs.ttml")
            .WithChannel("ORF");

        Assert.Equal("https://example.com/video.mp4", options.VideoUrl);
        Assert.Equal("/tmp/out.mkv", options.OutputPath);
        Assert.Equal("https://example.com/subs.ttml", options.SubtitleUrl);
        Assert.Equal("ORF", options.Channel);
    }
}
