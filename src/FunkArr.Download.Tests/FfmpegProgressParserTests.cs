using FunkArr.Download;

namespace FunkArr.Download.Tests;

public sealed class FfmpegProgressParserTests
{
    [Fact]
    public void Parse_complete_progress_block()
    {
        var block = new Dictionary<string, string>
        {
            ["out_time_us"] = "11360000",
            ["total_size"] = "11010048",
            ["speed"] = "1.5x",
            ["progress"] = "continue",
        };

        var result = FfmpegProgressParser.Parse(block);

        Assert.NotNull(result);
        Assert.Equal(11_360_000L, result.OutTimeUs);
        Assert.Equal(11_010_048L, result.TotalSize);
        Assert.Equal(1.5, result.Speed);
    }

    [Fact]
    public void Parse_end_block()
    {
        var block = new Dictionary<string, string>
        {
            ["out_time_us"] = "5348000000",
            ["total_size"] = "1632632832",
            ["speed"] = "1.0x",
            ["progress"] = "end",
        };

        var result = FfmpegProgressParser.Parse(block);

        Assert.NotNull(result);
        Assert.Equal(5_348_000_000L, result.OutTimeUs);
        Assert.Equal(1_632_632_832L, result.TotalSize);
        Assert.Equal(1.0, result.Speed);
    }

    [Fact]
    public void Parse_speed_na()
    {
        var block = new Dictionary<string, string>
        {
            ["out_time_us"] = "0",
            ["total_size"] = "0",
            ["speed"] = "N/A",
            ["progress"] = "continue",
        };

        var result = FfmpegProgressParser.Parse(block);

        Assert.NotNull(result);
        Assert.Equal(0.0, result.Speed);
    }

    [Fact]
    public void Parse_empty_block_returns_null()
    {
        var result = FfmpegProgressParser.Parse([]);

        Assert.Null(result);
    }

    [Fact]
    public void AccumulateLine_parses_key_value()
    {
        var block = new Dictionary<string, string>();

        FfmpegProgressParser.AccumulateLine(block, "out_time_us=11360000");
        FfmpegProgressParser.AccumulateLine(block, "speed=1.5x");

        Assert.Equal("11360000", block["out_time_us"]);
        Assert.Equal("1.5x", block["speed"]);
    }

    [Fact]
    public void AccumulateLine_ignores_malformed_lines()
    {
        var block = new Dictionary<string, string>();

        FfmpegProgressParser.AccumulateLine(block, "no-equals-sign");
        FfmpegProgressParser.AccumulateLine(block, "");

        Assert.Empty(block);
    }

    [Fact]
    public void IsBlockComplete_true_when_progress_key_present()
    {
        var block = new Dictionary<string, string> { ["progress"] = "continue" };

        Assert.True(FfmpegProgressParser.IsBlockComplete(block));
    }

    [Fact]
    public void IsBlockComplete_false_when_no_progress_key()
    {
        var block = new Dictionary<string, string> { ["out_time_us"] = "123" };

        Assert.False(FfmpegProgressParser.IsBlockComplete(block));
    }
}
