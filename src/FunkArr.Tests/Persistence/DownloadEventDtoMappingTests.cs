using FunkArr.DownloadClient;
using FunkArr.Persistence;

namespace FunkArr.Tests.Persistence;

public class DownloadEventDtoMappingTests
{
    [Fact]
    public void DownloadEnqueued_Roundtrip()
    {
        var evt = new DownloadEvents.DownloadEnqueued(
            "abc123", "https://example.com/video.mp4", "Test Show",
            "https://example.com/sub.srt", new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero));

        var result = DownloadEventDtoMapping.ToDomain(DownloadEventDtoMapping.ToDto(evt));

        Assert.Equal(evt, result);
    }

    [Fact]
    public void DownloadEnqueued_NullSubtitle_Roundtrip()
    {
        var evt = new DownloadEvents.DownloadEnqueued(
            "abc123", "https://example.com/video.mp4", "Test Show",
            null, new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero));

        var result = DownloadEventDtoMapping.ToDomain(DownloadEventDtoMapping.ToDto(evt));

        Assert.Equal(evt, result);
    }

    [Fact]
    public void DownloadStarted_Roundtrip()
    {
        var evt = new DownloadEvents.DownloadStarted("abc123");

        var result = DownloadEventDtoMapping.ToDomain(DownloadEventDtoMapping.ToDto(evt));

        Assert.Equal(evt, result);
    }

    [Fact]
    public void DownloadCompleted_Roundtrip()
    {
        var evt = new DownloadEvents.DownloadCompleted("abc123", "/tmp/abc123.mp4", "/tmp/abc123.srt");

        var result = DownloadEventDtoMapping.ToDomain(DownloadEventDtoMapping.ToDto(evt));

        Assert.Equal(evt, result);
    }

    [Fact]
    public void DownloadCompleted_NullSubtitle_Roundtrip()
    {
        var evt = new DownloadEvents.DownloadCompleted("abc123", "/tmp/abc123.mp4", null);

        var result = DownloadEventDtoMapping.ToDomain(DownloadEventDtoMapping.ToDto(evt));

        Assert.Equal(evt, result);
    }

    [Fact]
    public void DownloadFailed_Roundtrip()
    {
        var evt = new DownloadEvents.DownloadFailed("abc123", "Connection timed out");

        var result = DownloadEventDtoMapping.ToDomain(DownloadEventDtoMapping.ToDto(evt));

        Assert.Equal(evt, result);
    }

    [Fact]
    public void MuxingStarted_Roundtrip()
    {
        var evt = new DownloadEvents.MuxingStarted("abc123");

        var result = DownloadEventDtoMapping.ToDomain(DownloadEventDtoMapping.ToDto(evt));

        Assert.Equal(evt, result);
    }

    [Fact]
    public void MuxingCompleted_Roundtrip()
    {
        var evt = new DownloadEvents.MuxingCompleted("abc123", "/downloads/Test Show/Test Show.mkv");

        var result = DownloadEventDtoMapping.ToDomain(DownloadEventDtoMapping.ToDto(evt));

        Assert.Equal(evt, result);
    }

    [Fact]
    public void MuxingFailed_Roundtrip()
    {
        var evt = new DownloadEvents.MuxingFailed("abc123", "FFmpeg exited with code 1");

        var result = DownloadEventDtoMapping.ToDomain(DownloadEventDtoMapping.ToDto(evt));

        Assert.Equal(evt, result);
    }
}
