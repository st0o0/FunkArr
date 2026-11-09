using FFmpeg.Rx;
using FFmpeg.Rx.Codecs;
using FFmpeg.Rx.Models;
using FunkArr.Download.Ffmpeg;

namespace FunkArr.Download.Tests.Ffmpeg;

public sealed class RemuxerTests
{
    [Fact]
    public void BuildPipeline_direct_mp4_maps_video_and_audio_copy()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);
        var pipeline = FfmpegProcess.BuildPipeline(input);
        var output = pipeline.Output;

        Assert.NotNull(output.Streams);
        Assert.Equal(2, output.Streams.Count);

        Assert.Multiple(
            () => Assert.Equal(0, output.Streams[0].InputIndex),
            () => Assert.Equal(0, output.Streams[0].StreamIndex),
            () => Assert.Equal(StreamType.Video, output.Streams[0].Type),
            () => Assert.IsType<StreamCopy>(output.Streams[0].Codec));

        Assert.Multiple(
            () => Assert.Equal(0, output.Streams[1].InputIndex),
            () => Assert.Equal(0, output.Streams[1].StreamIndex),
            () => Assert.Equal(StreamType.Audio, output.Streams[1].Type),
            () => Assert.IsType<StreamCopy>(output.Streams[1].Codec));
    }

    [Fact]
    public void BuildPipeline_direct_mp4_no_bsf()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);
        var pipeline = FfmpegProcess.BuildPipeline(input);
        var output = pipeline.Output;

        Assert.True(
            output.CustomArgs is null ||
            !output.CustomArgs.Any(a => a.Contains("-bsf:a", StringComparison.Ordinal)));
    }

    [Fact]
    public void BuildPipeline_direct_mp4_no_subtitle_streams()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);
        var pipeline = FfmpegProcess.BuildPipeline(input);
        var output = pipeline.Output;

        Assert.NotNull(output.Streams);
        Assert.DoesNotContain(output.Streams, s => s.Type == StreamType.Subtitle);
    }

    [Fact]
    public void BuildPipeline_hls_includes_bsf_tolerance()
    {
        var input = new FfmpegInput("https://example.com/stream.m3u8", null, "/tmp/out.mkv", null, null, true);
        var pipeline = FfmpegProcess.BuildPipeline(input);
        var output = pipeline.Output;

        Assert.NotNull(output.CustomArgs);
        Assert.Contains("-bsf:a aac_adtstoasc=no_validation=1", output.CustomArgs);
    }

    [Fact]
    public void BuildPipeline_with_subtitle_includes_subtitle_stream()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", "/tmp/subtitle.srt", "/tmp/out.mkv", null, null, false);
        var pipeline = FfmpegProcess.BuildPipeline(input);
        var output = pipeline.Output;

        Assert.NotNull(output.Streams);
        Assert.Equal(3, output.Streams.Count);

        var subStream = output.Streams[2];
        Assert.Multiple(
            () => Assert.Equal(1, subStream.InputIndex),
            () => Assert.Null(subStream.StreamIndex),
            () => Assert.Equal(StreamType.Subtitle, subStream.Type),
            () =>
            {
                var codec = Assert.IsType<SubtitleEncode>(subStream.Codec);
                Assert.Equal("srt", codec.Codec.Name);
            },
            () => Assert.Equal("0", subStream.Disposition));
    }

    [Fact]
    public void BuildPipeline_with_subtitle_default_language_deu()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", "/tmp/subtitle.srt", "/tmp/out.mkv", null, null, false);
        var pipeline = FfmpegProcess.BuildPipeline(input);
        var output = pipeline.Output;

        Assert.NotNull(output.Streams);
        var subStream = output.Streams[2];
        Assert.NotNull(subStream.Metadata);
        var langEntry = Assert.Single(subStream.Metadata);
        Assert.Equal("language", langEntry.Key);
        Assert.Equal("deu", langEntry.Value);
    }

    [Fact]
    public void BuildPipeline_with_subtitle_custom_language()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", "/tmp/subtitle.srt", "/tmp/out.mkv", null, "eng", false);
        var pipeline = FfmpegProcess.BuildPipeline(input);
        var output = pipeline.Output;

        Assert.NotNull(output.Streams);
        var subStream = output.Streams[2];
        Assert.NotNull(subStream.Metadata);
        var langEntry = Assert.Single(subStream.Metadata);
        Assert.Equal("eng", langEntry.Value);
    }

    [Fact]
    public void BuildPipeline_hls_with_subtitle_includes_both_bsf_and_subtitle()
    {
        var input = new FfmpegInput("https://example.com/stream.m3u8", "/tmp/subtitle.srt", "/tmp/out.mkv", null, "deu", true);
        var pipeline = FfmpegProcess.BuildPipeline(input);
        var output = pipeline.Output;

        Assert.NotNull(output.CustomArgs);
        Assert.Contains("-bsf:a aac_adtstoasc=no_validation=1", output.CustomArgs);

        Assert.NotNull(output.Streams);
        Assert.Contains(output.Streams, s => s.Type == StreamType.Subtitle);
    }

    [Fact]
    public void BuildInputs_includes_user_agent()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);
        var inputs = FfmpegProcess.BuildInputs(input);

        var urlInput = Assert.IsType<UrlInputConfig>(Assert.Single(inputs));
        Assert.NotNull(urlInput.Options);
        Assert.NotNull(urlInput.Options.CustomArgs);
        Assert.Contains(urlInput.Options.CustomArgs, a => a.Contains("-user_agent", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildInputs_with_proxy_includes_http_proxy()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", "http://proxy:8888", null, false);
        var inputs = FfmpegProcess.BuildInputs(input);

        var urlInput = Assert.IsType<UrlInputConfig>(inputs[0]);
        Assert.NotNull(urlInput.Options);
        Assert.NotNull(urlInput.Options.CustomArgs);
        Assert.Contains(urlInput.Options.CustomArgs, a => a.Contains("-http_proxy http://proxy:8888", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildInputs_without_proxy_omits_http_proxy()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);
        var inputs = FfmpegProcess.BuildInputs(input);

        var urlInput = Assert.IsType<UrlInputConfig>(Assert.Single(inputs));
        Assert.NotNull(urlInput.Options);
        Assert.NotNull(urlInput.Options.CustomArgs);
        Assert.DoesNotContain(urlInput.Options.CustomArgs, a => a.Contains("-http_proxy", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildInputs_with_subtitle_adds_file_input()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", "/tmp/subtitle.srt", "/tmp/out.mkv", null, null, false);
        var inputs = FfmpegProcess.BuildInputs(input);

        Assert.Equal(2, inputs.Length);
        Assert.IsType<UrlInputConfig>(inputs[0]);
        var fileInput = Assert.IsType<FileInputConfig>(inputs[1]);
        Assert.Equal("/tmp/subtitle.srt", fileInput.Path);
    }

    [Fact]
    public void BuildPipeline_includes_progress_global_option()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);
        var pipeline = FfmpegProcess.BuildPipeline(input);

        Assert.NotNull(pipeline.GlobalOptions);
        Assert.Contains("-progress pipe:1", pipeline.GlobalOptions);
    }

    [Fact]
    public void BuildPipeline_output_destination_is_file()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);
        var pipeline = FfmpegProcess.BuildPipeline(input);

        var dest = Assert.IsType<FileDestination>(pipeline.Output.Destination);
        Assert.Equal("/tmp/out.mkv", dest.Path);
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
