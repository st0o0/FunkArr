using FunkArr.Messages.Download;

namespace FunkArr.Api.Tests;

public sealed class DownloadApiEndpointTests
{
    [Fact]
    public void Queue_response_maps_processing_item_with_progress()
    {
        var item = new QueueItem(
            DownloadId: Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
            Title: "Tagesschau 20:00",
            Status: DownloadStatus.Processing,
            TotalBytes: 245_000_000,
            BytesDownloaded: 176_400_000,
            CurrentTimeUs: 36_000_000,
            TotalDuration: 50,
            Speed: 1.0,
            Category: "tv");

        var result = QueueApiEndpoints.ToQueueItem(item);

        Assert.Equal("Processing", result.Status);
        Assert.Equal(72, result.Percentage);
        Assert.True(result.Speed > 0);
        Assert.NotEqual("00:00:00", result.Eta);
    }

    [Fact]
    public void Queue_response_maps_queued_item_with_zero_progress()
    {
        var item = new QueueItem(
            DownloadId: Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"),
            Title: "heute journal",
            Status: DownloadStatus.Queued,
            TotalBytes: 180_000_000,
            BytesDownloaded: 0,
            CurrentTimeUs: 0,
            TotalDuration: 0,
            Speed: 0,
            Category: "tv");

        var result = QueueApiEndpoints.ToQueueItem(item);

        Assert.Equal("Queued", result.Status);
        Assert.Equal(0, result.Percentage);
        Assert.Equal(0, result.Speed);
        Assert.Equal("00:00:00", result.Eta);
    }

    [Fact]
    public void Queue_response_clamps_percentage_to_100()
    {
        var item = new QueueItem(
            DownloadId: Guid.NewGuid(),
            Title: "Test",
            Status: DownloadStatus.Processing,
            TotalBytes: 100,
            BytesDownloaded: 100,
            CurrentTimeUs: 60_000_000,
            TotalDuration: 50,
            Speed: 1.0,
            Category: "tv");

        var result = QueueApiEndpoints.ToQueueItem(item);

        Assert.True(result.Percentage <= 100);
    }

    [Fact]
    public void History_response_maps_completed_item()
    {
        var item = new HistoryItem(
            DownloadId: Guid.NewGuid(),
            Title: "Tagesschau",
            Category: "tv",
            TotalBytes: 245_000_000,
            DownloadTimeSeconds: 185,
            RelativePath: "/downloads/tagesschau.mkv",
            Status: DownloadStatus.Completed,
            FailMessage: "",
            CompletedAt: 1725300000);

        var result = QueueApiEndpoints.ToHistoryItem(item);

        Assert.Equal("Completed", result.Status);
        Assert.Equal("/downloads/tagesschau.mkv", result.RelativePath);
        Assert.Null(result.FailMessage);
    }

    [Fact]
    public void History_response_maps_failed_item()
    {
        var item = new HistoryItem(
            DownloadId: Guid.NewGuid(),
            Title: "Panorama",
            Category: "tv",
            TotalBytes: 98_000_000,
            DownloadTimeSeconds: 0,
            RelativePath: "",
            Status: DownloadStatus.Failed,
            FailMessage: "FFmpeg exited with code 1",
            CompletedAt: 1725300000);

        var result = QueueApiEndpoints.ToHistoryItem(item);

        Assert.Equal("Failed", result.Status);
        Assert.Null(result.RelativePath);
        Assert.Equal("FFmpeg exited with code 1", result.FailMessage);
    }

    [Fact]
    public void Queue_response_preserves_total_slots()
    {
        var queueResult = new QueueResult([], 5, 0);

        var response = QueueApiEndpoints.ToQueueResponse(queueResult);

        Assert.Empty(response.Items);
        Assert.Equal(5, response.TotalSlots);
    }

    [Fact]
    public void History_completed_at_is_iso8601()
    {
        var item = new HistoryItem(
            DownloadId: Guid.NewGuid(),
            Title: "Test",
            Category: "tv",
            TotalBytes: 100,
            DownloadTimeSeconds: 10,
            RelativePath: "/test.mkv",
            Status: DownloadStatus.Completed,
            FailMessage: "",
            CompletedAt: 1725300000);

        var result = QueueApiEndpoints.ToHistoryItem(item);

        Assert.Contains("T", result.CompletedAt);
        Assert.True(DateTimeOffset.TryParse(result.CompletedAt, out _));
    }
}
