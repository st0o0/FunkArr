using FunkArr.DownloadClient;

namespace FunkArr.Tests.DownloadClient;

public class FfmpegProgressParserTests
{
    [Fact]
    public void Parse_ValidTimeAndSpeed_ExtractsValues()
    {
        var line = "frame=  120 fps= 25 q=-1.0 size=    5120kB time=00:15:30.45 bitrate=  45.2kbits/s speed=2.5x";

        var result = FfmpegProgressParser.Parse(line);

        Assert.NotNull(result);
        Assert.Equal(930, result.Value.ElapsedSeconds);
        Assert.Equal(2.5, result.Value.Speed);
    }

    [Fact]
    public void Parse_TimeOnly_ExtractsWithZeroSpeed()
    {
        var line = "time=01:00:00.00 bitrate=1000kbits/s";

        var result = FfmpegProgressParser.Parse(line);

        Assert.NotNull(result);
        Assert.Equal(3600, result.Value.ElapsedSeconds);
        Assert.Equal(0.0, result.Value.Speed);
    }

    [Fact]
    public void Parse_NoTimeInfo_ReturnsNull()
    {
        var line = "Press [q] to stop, [?] for help";

        var result = FfmpegProgressParser.Parse(line);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_ZeroTime_ReturnsZero()
    {
        var line = "time=00:00:00.00 speed=1.0x";

        var result = FfmpegProgressParser.Parse(line);

        Assert.NotNull(result);
        Assert.Equal(0, result.Value.ElapsedSeconds);
        Assert.Equal(1.0, result.Value.Speed);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsNull()
    {
        var result = FfmpegProgressParser.Parse("");

        Assert.Null(result);
    }
}
