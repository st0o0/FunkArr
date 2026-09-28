using FunkArr.Api.Extensions;
using FunkArr.Api.Models;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using MediaType = FunkArr.Messages.MediaType;

namespace FunkArr.Api.Tests.Extensions;

public sealed class DownloadMappingExtensionsTests
{
    [Fact]
    public void Queue_response_maps_next_window_and_pause_state()
    {
        var nextWindow = new DateTimeOffset(2025, 3, 15, 22, 0, 0, TimeSpan.Zero);
        var queueResult = new QueueResult([], 4, 0, true, false, nextWindow);

        var response = queueResult.ToApi();

        Assert.Empty(response.Items);
        Assert.Equal(4, response.TotalSlots);
        Assert.Equal(0, response.ActiveCount);
        Assert.Equal(0, response.QueuedCount);
        Assert.True(response.IsPaused);
        Assert.False(response.IsScheduleActive);
        Assert.NotNull(response.NextWindow);
        Assert.Equal(nextWindow, response.NextWindow);
    }

    [Fact]
    public void Queue_item_with_initialized_phase_derives_video_download_when_bytes_remain()
    {
        var item = new QueueItem(
            DownloadId: Guid.NewGuid(),
            Title: "Tatort",
            Status: DownloadStatus.Processing,
            Channel: "ARD",
            HasSubtitles: false,
            TotalBytes: 500_000_000,
            Progress: new DownloadProgress(100_000_000, 5_000_000, 1.0),
            TotalDuration: 90,
            Category: MediaType.Show,
            Priority: DownloadPriority.Normal,
            Phase: DownloadPhase.Initialized,
            Attempt: 1);

        var result = item.ToApi();

        Assert.Equal("videodownload", result.Phase);
        Assert.Equal(20, result.Percentage);
    }

    [Fact]
    public void Queue_item_with_initialized_phase_derives_remuxing_when_download_complete()
    {
        var item = new QueueItem(
            DownloadId: Guid.NewGuid(),
            Title: "Tatort",
            Status: DownloadStatus.Processing,
            Channel: "ARD",
            HasSubtitles: false,
            TotalBytes: 500_000_000,
            Progress: new DownloadProgress(500_000_000, 45_000_000, 1.0),
            TotalDuration: 90,
            Category: MediaType.Show,
            Priority: DownloadPriority.Normal,
            Phase: DownloadPhase.Initialized,
            Attempt: 1);

        var result = item.ToApi();

        Assert.Equal("remuxing", result.Phase);
        Assert.Equal(50, result.Percentage);
    }

    [Fact]
    public void History_result_maps_collection_and_total_count()
    {
        var items = new[]
        {
            new HistoryItem(Guid.NewGuid(),
                new DownloadCompletion("Tagesschau", MediaType.Show,
                    245_000_000, DownloadStatus.Completed, "/tagesschau.mkv", null, 120, 1725300000)),
            new HistoryItem(Guid.NewGuid(),
                new DownloadCompletion("Panorama", MediaType.Show,
                    98_000_000, DownloadStatus.Failed, null, "Timeout", 0, 1725400000))
        };
        var historyResult = new HistoryResult(items, 42);

        var response = historyResult.ToApi();

        Assert.Equal(42, response.TotalItems);
        Assert.Equal(2, response.Items.Length);
        Assert.Equal(HistoryStatus.Completed, response.Items[0].Status);
        Assert.Equal(HistoryStatus.Failed, response.Items[1].Status);
    }

    [Fact]
    public void Queue_item_maps_high_priority_as_string()
    {
        var item = new QueueItem(
            DownloadId: Guid.NewGuid(),
            Title: "Brennpunkt",
            Status: DownloadStatus.Queued,
            Channel: "ARD",
            HasSubtitles: false,
            TotalBytes: 200_000_000,
            Progress: new DownloadProgress(0, 0, 0),
            TotalDuration: 30,
            Category: MediaType.Show,
            Priority: DownloadPriority.High,
            Phase: DownloadPhase.Initialized,
            Attempt: 0);

        var result = item.ToApi();

        Assert.Equal("High", result.Priority);
    }

    [Fact]
    public void Queue_item_maps_movie_category()
    {
        var item = new QueueItem(
            DownloadId: Guid.NewGuid(),
            Title: "Der Alte",
            Status: DownloadStatus.Processing,
            Channel: "ZDF",
            HasSubtitles: true,
            TotalBytes: 1_500_000_000,
            Progress: new DownloadProgress(750_000_000, 30_000_000, 1.0),
            TotalDuration: 120,
            Category: MediaType.Movie,
            Priority: DownloadPriority.Low,
            Phase: DownloadPhase.VideoDownload,
            Attempt: 1);

        var result = item.ToApi();

        Assert.Equal(Models.MediaType.Movie, result.Category);
        Assert.Equal("Low", result.Priority);
    }
}
