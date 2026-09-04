using FunkArr.Download;

namespace FunkArr.Download.Tests;

public sealed class FfmpegRunnerTests
{
    [Fact]
    public void ParseProgressLine_complete_block_invokes_callback()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegRunner.ParseProgressLine("out_time_us=11360000", block, p => result = p);
        FfmpegRunner.ParseProgressLine("total_size=11010048", block, p => result = p);
        FfmpegRunner.ParseProgressLine("speed=1.5x", block, p => result = p);
        FfmpegRunner.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(11_010_048L, result.TotalSize);
        Assert.Equal(11_360_000L, result.OutTimeUs);
        Assert.Equal(1.5, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_clears_block_after_emit()
    {
        var block = new Dictionary<string, string>();

        FfmpegRunner.ParseProgressLine("total_size=100", block, _ => { });
        FfmpegRunner.ParseProgressLine("progress=continue", block, _ => { });

        Assert.Empty(block);
    }

    [Fact]
    public void ParseProgressLine_speed_na_yields_zero()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegRunner.ParseProgressLine("out_time_us=0", block, p => result = p);
        FfmpegRunner.ParseProgressLine("total_size=0", block, p => result = p);
        FfmpegRunner.ParseProgressLine("speed=N/A", block, p => result = p);
        FfmpegRunner.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(0.0, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_ignores_malformed_lines()
    {
        var block = new Dictionary<string, string>();

        FfmpegRunner.ParseProgressLine("no-equals-sign", block, _ => Assert.Fail("Should not invoke"));
        FfmpegRunner.ParseProgressLine("", block, _ => Assert.Fail("Should not invoke"));

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

        FfmpegRunner.ParseProgressLine("total_size=999999", block, p => result = p);
        FfmpegRunner.ParseProgressLine("out_time_us=43200000000", block, p => result = p);
        FfmpegRunner.ParseProgressLine("speed=2.5x", block, p => result = p);
        FfmpegRunner.ParseProgressLine("progress=end", block, p => result = p);

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

        FfmpegRunner.ParseProgressLine("progress=continue", block, p => result = p);

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

        FfmpegRunner.ParseProgressLine("total_size=5000", block, p => result = p);
        FfmpegRunner.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(5000L, result.TotalSize);
        Assert.Equal(0L, result.OutTimeUs);
        Assert.Equal(0.0, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_non_progress_key_does_not_emit()
    {
        var block = new Dictionary<string, string>();

        FfmpegRunner.ParseProgressLine("total_size=100", block, _ => Assert.Fail("Should not invoke"));
        FfmpegRunner.ParseProgressLine("out_time_us=200", block, _ => Assert.Fail("Should not invoke"));
        FfmpegRunner.ParseProgressLine("speed=1.0x", block, _ => Assert.Fail("Should not invoke"));

        Assert.Equal(3, block.Count);
    }

    [Fact]
    public void ParseProgressLine_handles_whitespace_around_equals()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegRunner.ParseProgressLine("total_size =  4096  ", block, p => result = p);
        FfmpegRunner.ParseProgressLine("progress = continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(4096L, result.TotalSize);
    }

    [Fact]
    public void ParseProgressLine_speed_without_x_suffix()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegRunner.ParseProgressLine("speed=3.14", block, p => result = p);
        FfmpegRunner.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(3.14, result.Speed);
    }

    [Fact]
    public void ParseProgressLine_negative_total_size_parsed()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegRunner.ParseProgressLine("total_size=-1", block, p => result = p);
        FfmpegRunner.ParseProgressLine("progress=continue", block, p => result = p);

        Assert.NotNull(result);
        Assert.Equal(-1L, result.TotalSize);
    }

    [Fact]
    public void ParseProgressLine_non_numeric_total_size_yields_zero()
    {
        var block = new Dictionary<string, string>();
        ProgressUpdate? result = null;

        FfmpegRunner.ParseProgressLine("total_size=abc", block, p => result = p);
        FfmpegRunner.ParseProgressLine("progress=continue", block, p => result = p);

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
            FfmpegRunner.ParseProgressLine(line, block, p => updates.Add(p));
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
    public void BuildArguments_video_only_includes_progress_pipe()
    {
        var processor = FfmpegRunner.BuildArguments(
            "https://example.com/video.mp4", null, "/tmp/out.mkv");

        var args = processor.Arguments;

        Assert.Contains("-progress pipe:1", args);
        Assert.Contains("-codec copy", args);
        Assert.DoesNotContain("-c:s srt", args);
    }

    [Fact]
    public void BuildArguments_with_subtitle_includes_progress_and_srt()
    {
        var processor = FfmpegRunner.BuildArguments(
            "https://example.com/video.mp4",
            "https://example.com/subs.xml",
            "/tmp/out.mkv");

        var args = processor.Arguments;

        Assert.Contains("-progress pipe:1", args);
        Assert.Contains("-c:s srt", args);
        Assert.Contains("-metadata:s:s:0 language=deu", args);
    }

    private static void FeedBlock(
        Dictionary<string, string> block, List<ProgressUpdate> updates, params string[] lines)
    {
        foreach (var line in lines)
        {
            FfmpegRunner.ParseProgressLine(line, block, p => updates.Add(p));
        }
    }
}
