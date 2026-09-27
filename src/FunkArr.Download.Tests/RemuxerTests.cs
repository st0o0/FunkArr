using FunkArr.Messages.Download;

namespace FunkArr.Download.Tests;

public sealed class RemuxerTests
{
    [Fact]
    public void ParseProgressLine_complete_block_invokes_callback()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegProcess.ParseProgressLine("out_time_us=11360000", block, p => result = p);
        FfmpegProcess.ParseProgressLine("total_size=11010048", block, p => result = p);
        FfmpegProcess.ParseProgressLine("speed=1.5x", block, p => result = p);
        FfmpegProcess.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(11_010_048L, result.TotalSize);
        Assert.Equal(11_360_000L, result.OutTimeUs);
        Assert.Equal(1.5, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_clears_block_after_emit()
    {
        var block = new Dictionary<string, string>();

        FfmpegProcess.ParseProgressLine("total_size=100", block, _ => { });
        FfmpegProcess.ParseProgressLine("progress=continue", block, _ => { });

        Assert.Empty(block);
    }

    [Fact]
    public void ParseProgressLine_speed_na_yields_zero()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegProcess.ParseProgressLine("out_time_us=0", block, p => result = p);
        FfmpegProcess.ParseProgressLine("total_size=0", block, p => result = p);
        FfmpegProcess.ParseProgressLine("speed=N/A", block, p => result = p);
        FfmpegProcess.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(0.0, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_ignores_malformed_lines()
    {
        var block = new Dictionary<string, string>();

        FfmpegProcess.ParseProgressLine("no-equals-sign", block, _ => Assert.Fail("Should not invoke"));
        FfmpegProcess.ParseProgressLine("", block, _ => Assert.Fail("Should not invoke"));

        Assert.Empty(block);
    }

    [Fact]
    public void ParseProgressLine_multiple_blocks_emit_independently()
    {
        var block = new Dictionary<string, string>();
        var updates = new List<ProgressUpdate>();

        FeedBlock(block, updates, "total_size=1000", "out_time_us=500000", "speed=1.0x", "progress=continue");
        FeedBlock(block, updates, "total_size=2000", "out_time_us=1000000", "speed=1.2x", "progress=continue");
        FeedBlock(block, updates, "total_size=3000", "out_time_us=1500000", "speed=0.9x", "progress=end");

        Assert.Equal(3, updates.Count);
        Assert.Equal(1000L, updates[0].TotalSize);
        Assert.Equal(2000L, updates[1].TotalSize);
        Assert.Equal(3000L, updates[2].TotalSize);
    }

    [Fact]
    public void ParseProgressLine_progress_end_emits_final_update()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegProcess.ParseProgressLine("total_size=999999", block, p => result = p);
        FfmpegProcess.ParseProgressLine("out_time_us=43200000000", block, p => result = p);
        FfmpegProcess.ParseProgressLine("speed=2.5x", block, p => result = p);
        FfmpegProcess.ParseProgressLine("progress=end", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(999_999L, result.TotalSize);
        Assert.Equal(43_200_000_000L, result.OutTimeUs);
        Assert.Equal(2.5, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_missing_fields_default_to_zero()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegProcess.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(0L, result.TotalSize);
        Assert.Equal(0L, result.OutTimeUs);
        Assert.Equal(0.0, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_partial_fields_zero_filled()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegProcess.ParseProgressLine("total_size=5000", block, p => result = p);
        FfmpegProcess.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(5000L, result.TotalSize);
        Assert.Equal(0L, result.OutTimeUs);
        Assert.Equal(0.0, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_non_progress_key_does_not_emit()
    {
        var block = new Dictionary<string, string>();

        FfmpegProcess.ParseProgressLine("total_size=100", block, _ => Assert.Fail("Should not invoke"));
        FfmpegProcess.ParseProgressLine("out_time_us=200", block, _ => Assert.Fail("Should not invoke"));
        FfmpegProcess.ParseProgressLine("speed=1.0x", block, _ => Assert.Fail("Should not invoke"));

        Assert.Equal(3, block.Count);
    }

    [Fact]
    public void ParseProgressLine_handles_whitespace_around_equals()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegProcess.ParseProgressLine("total_size =  4096  ", block, p => result = p);
        FfmpegProcess.ParseProgressLine("progress = continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(4096L, result.TotalSize);
    }

    [Fact]
    public void ParseProgressLine_speed_without_x_suffix()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegProcess.ParseProgressLine("speed=3.14", block, p => result = p);
        FfmpegProcess.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(3.14, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_negative_total_size_parsed()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegProcess.ParseProgressLine("total_size=-1", block, p => result = p);
        FfmpegProcess.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(-1L, result.TotalSize);
    }

    [Fact]
    public void ParseProgressLine_non_numeric_total_size_yields_zero()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegProcess.ParseProgressLine("total_size=abc", block, p => result = p);
        FfmpegProcess.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(0L, result.TotalSize);
    }

    [Fact]
    public void ParseProgressLine_realistic_ffmpeg_output()
    {
        var block = new Dictionary<string, string>();
        var updates = new List<ProgressUpdate>();

        var lines = new[]
        {
            "bitrate= 4893.5kbits/s",
            "total_size=5890048",
            "out_time_us=9630000",
            "out_time_ms=9630000",
            "out_time=00:00:09.630000",
            "dup_frames=0",
            "drop_frames=0",
            "speed=1.93x",
            "progress=continue",
            "bitrate= 4901.2kbits/s",
            "total_size=11780096",
            "out_time_us=19230000",
            "out_time_ms=19230000",
            "out_time=00:00:19.230000",
            "dup_frames=0",
            "drop_frames=0",
            "speed=1.95x",
            "progress=continue",
        };

        foreach (var line in lines)
        {
            FfmpegProcess.ParseProgressLine(line, block, p => updates.Add(p));
        }

        Assert.Equal(2, updates.Count);

        Assert.Equal(5_890_048L, updates[0].TotalSize);
        Assert.Equal(9_630_000L, updates[0].OutTimeUs);
        Assert.Equal(1.93, updates[0].Speed);

        Assert.Equal(11_780_096L, updates[1].TotalSize);
        Assert.Equal(19_230_000L, updates[1].OutTimeUs);
        Assert.Equal(1.95, updates[1].Speed);
    }

    [Fact]
    public void BuildArguments_direct_mp4_maps_video_and_audio()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);
        var args = FfmpegProcess.BuildArguments(input).Arguments;

        Assert.Contains("-map 0:v:0", args);
        Assert.Contains("-map 0:a:0", args);
        Assert.Contains("-c:v copy", args);
        Assert.Contains("-c:a copy", args);
        Assert.Contains("-progress pipe:1", args);
        Assert.DoesNotContain("-bsf:a", args);
        Assert.DoesNotContain("-c:s srt", args);
    }

    [Fact]
    public void BuildArguments_hls_includes_bsf_tolerance()
    {
        var input = new FfmpegInput("https://example.com/stream.m3u8", null, "/tmp/out.mkv", null, null, true);
        var args = FfmpegProcess.BuildArguments(input).Arguments;

        Assert.Contains("-map 0:v:0", args);
        Assert.Contains("-map 0:a:0", args);
        Assert.Contains("-c:v copy", args);
        Assert.Contains("-c:a copy", args);
        Assert.Contains("-bsf:a aac_adtstoasc=no_validation=1", args);
    }

    [Fact]
    public void BuildArguments_with_subtitle_includes_srt_args()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", "/tmp/subtitle.srt", "/tmp/out.mkv", null, null, false);
        var args = FfmpegProcess.BuildArguments(input).Arguments;

        Assert.Contains("-c:s srt", args);
        Assert.Contains("-disposition:s:0 0", args);
        Assert.Contains("-metadata:s:s:0 language=deu", args);
    }

    [Fact]
    public void BuildArguments_with_subtitle_language_uses_parameter()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", "/tmp/subtitle.srt", "/tmp/out.mkv", null, "eng", false);
        var args = FfmpegProcess.BuildArguments(input).Arguments;

        Assert.Contains("-metadata:s:s:0 language=eng", args);
        Assert.DoesNotContain("language=deu", args);
    }

    [Fact]
    public void BuildArguments_with_null_subtitle_language_defaults_to_deu()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", "/tmp/subtitle.srt", "/tmp/out.mkv", null, null, false);
        var args = FfmpegProcess.BuildArguments(input).Arguments;

        Assert.Contains("-metadata:s:s:0 language=deu", args);
    }

    [Fact]
    public void BuildArguments_hls_with_subtitle_includes_both_bsf_and_srt()
    {
        var input = new FfmpegInput("https://example.com/stream.m3u8", "/tmp/subtitle.srt", "/tmp/out.mkv", null, "deu", true);
        var args = FfmpegProcess.BuildArguments(input).Arguments;

        Assert.Contains("-bsf:a aac_adtstoasc=no_validation=1", args);
        Assert.Contains("-c:s srt", args);
        Assert.Contains("-disposition:s:0 0", args);
    }

    [Fact]
    public void BuildArguments_with_proxy_includes_http_proxy_before_input()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", "http://proxy-at:8888", null, false);
        var args = FfmpegProcess.BuildArguments(input).Arguments;

        Assert.Contains("-http_proxy http://proxy-at:8888", args);
        var proxyIndex = args.IndexOf("-http_proxy", StringComparison.Ordinal);
        var inputIndex = args.IndexOf("-i \"https://example.com/video.mp4\"", StringComparison.Ordinal);
        Assert.True(proxyIndex < inputIndex, "proxy argument must appear before -i");
    }

    [Fact]
    public void BuildArguments_without_proxy_omits_http_proxy()
    {
        var input = new FfmpegInput("https://example.com/video.mp4", null, "/tmp/out.mkv", null, null, false);
        var args = FfmpegProcess.BuildArguments(input).Arguments;

        Assert.DoesNotContain("-http_proxy", args);
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

    [Fact]
    public void ExtractError_http_403()
    {
        var stderr = "ffmpeg version 6.1.1\nlibavutil 58.29\n[https] HTTP error 403 Forbidden\nError opening input file https://example.com/video.mp4\nError opening input files: Server returned 403 Forbidden (access denied)";
        var result = FfmpegProcess.ExtractError(stderr);
        Assert.Equal("Error opening input files: Server returned 403 Forbidden (access denied)", result);
    }

    [Fact]
    public void ExtractError_null_returns_empty()
    {
        Assert.Equal("", FfmpegProcess.ExtractError(null));
        Assert.Equal("", FfmpegProcess.ExtractError(""));
        Assert.Equal("", FfmpegProcess.ExtractError("   "));
    }

    [Fact]
    public void ExtractError_unknown_pattern_returns_last_line()
    {
        var stderr = "ffmpeg version 6.1.1\nSome unknown error happened";
        Assert.Equal("Some unknown error happened", FfmpegProcess.ExtractError(stderr));
    }

    [Fact]
    public void ExtractError_server_returned()
    {
        var stderr = "ffmpeg version 6.1.1\nlots of build info\nServer returned 404 Not Found";
        Assert.Equal("Server returned 404 Not Found", FfmpegProcess.ExtractError(stderr));
    }

    [Fact]
    public void ClassifyFailure_503_is_transient()
    {
        Assert.Equal(FailureKind.Transient, FfmpegProcess.ClassifyFailure("Server returned 503"));
    }

    [Fact]
    public void ClassifyFailure_timeout_is_transient()
    {
        Assert.Equal(FailureKind.Transient, FfmpegProcess.ClassifyFailure("Connection timed out"));
    }

    [Fact]
    public void ClassifyFailure_connection_reset_is_transient()
    {
        Assert.Equal(FailureKind.Transient, FfmpegProcess.ClassifyFailure("Connection reset by peer"));
    }

    [Fact]
    public void ClassifyFailure_404_is_permanent()
    {
        Assert.Equal(FailureKind.Permanent, FfmpegProcess.ClassifyFailure("Server returned 404 Not Found"));
    }

    [Fact]
    public void ClassifyFailure_null_is_permanent()
    {
        Assert.Equal(FailureKind.Permanent, FfmpegProcess.ClassifyFailure(null));
    }

    [Fact]
    public void ClassifyFailure_empty_is_permanent()
    {
        Assert.Equal(FailureKind.Permanent, FfmpegProcess.ClassifyFailure(""));
    }

    [Fact]
    public void ClassifyFailure_unknown_error_is_permanent()
    {
        Assert.Equal(FailureKind.Permanent, FfmpegProcess.ClassifyFailure("Some unknown error"));
    }

    private static void FeedBlock(
        Dictionary<string, string> block, List<ProgressUpdate> updates, params string[] lines)
    {
        foreach (var line in lines)
        {
            FfmpegProcess.ParseProgressLine(line, block, p => updates.Add(p));
        }
    }
}
