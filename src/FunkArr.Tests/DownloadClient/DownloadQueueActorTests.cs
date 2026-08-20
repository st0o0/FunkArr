using FunkArr.DownloadClient;

namespace FunkArr.Tests.DownloadClient;

public class DownloadEventsTests
{
    [Fact]
    public void DownloadEnqueued_ContainsAllFields()
    {
        var evt = new DownloadEvents.DownloadEnqueued(
            "abc123", "https://example.com/video.mp4", "Test.S01E01",
            "https://example.com/sub.srt", DateTimeOffset.UtcNow);

        Assert.Equal("abc123", evt.NzoId);
        Assert.Equal("https://example.com/video.mp4", evt.DownloadUrl);
        Assert.Equal("Test.S01E01", evt.Title);
        Assert.Equal("https://example.com/sub.srt", evt.SubtitleUrl);
    }

    [Fact]
    public void DownloadJob_DefaultsToQueued()
    {
        var job = new DownloadJob
        {
            NzoId = "test",
            DownloadUrl = "https://example.com/video.mp4",
            Title = "Test.S01E01",
            EnqueuedAt = DateTimeOffset.UtcNow,
        };

        Assert.Equal(DownloadStatus.Queued, job.Status);
        Assert.Equal(0, job.ProgressPercent);
        Assert.Null(job.OutputPath);
        Assert.Null(job.CompletedAt);
    }

    [Fact]
    public void DownloadJob_WithRecordUpdate()
    {
        var job = new DownloadJob
        {
            NzoId = "test",
            DownloadUrl = "https://example.com/video.mp4",
            Title = "Test.S01E01",
            EnqueuedAt = DateTimeOffset.UtcNow,
        };

        var updated = job with
        {
            Status = DownloadStatus.Downloading,
            ProgressPercent = 50.0,
            DownloadedBytes = 500_000_000,
            TotalBytes = 1_000_000_000,
        };

        Assert.Equal(DownloadStatus.Downloading, updated.Status);
        Assert.Equal(50.0, updated.ProgressPercent);
        Assert.Equal("test", updated.NzoId);
    }
}
