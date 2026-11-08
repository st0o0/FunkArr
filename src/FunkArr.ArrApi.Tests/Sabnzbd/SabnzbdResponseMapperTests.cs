using FunkArr.ArrApi.Sabnzbd.Models;
using FunkArr.ArrApi.Sabnzbd.Services;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;

namespace FunkArr.ArrApi.Tests.Sabnzbd;

public sealed class SabnzbdResponseMapperTests
{
    [Fact]
    public void BuildQueueSlot_maps_processing_item()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test Download", DownloadStatus.Processing,
            "ARD", false, 1_048_576_000, new DownloadProgress(524_288_000, 30_000_000, 1.0),
            60, MediaType.Show, DownloadPriority.Normal, DownloadPhase.VideoDownload, 1);

        var slot = SabnzbdResponseMapper.BuildQueueSlot(item, 0);

        Assert.Equal(SabnzbdQueueStatus.Downloading, slot.Status);
        Assert.Equal("Test Download", slot.Filename);
        Assert.Equal("tv", slot.Cat);
        Assert.Equal(0, slot.Index);
        Assert.Equal("1000.0", slot.Mb);
        Assert.Equal("500.0", slot.Mbleft);
    }

    [Fact]
    public void BuildQueueSlot_maps_queued_item()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Queued", DownloadStatus.Queued,
            "ZDF", false, 500_000_000, new DownloadProgress(0, 0, 0),
            0, MediaType.Movie, DownloadPriority.High, DownloadPhase.Initialized, 0);

        var slot = SabnzbdResponseMapper.BuildQueueSlot(item, 3);

        Assert.Equal(SabnzbdQueueStatus.Queued, slot.Status);
        Assert.Equal(3, slot.Index);
        Assert.Equal("movies", slot.Cat);
    }

    [Fact]
    public void BuildHistorySlot_maps_completed_item()
    {
        var item = new HistoryItem(
            Guid.NewGuid(), new DownloadCompletion("Completed Show", MediaType.Show,
            1_000_000, DownloadStatus.Completed, "shows/show.mkv", null, 120, 1719360000));

        var slot = SabnzbdResponseMapper.BuildHistorySlot(item, "/data/complete");

        Assert.Equal("Completed Show", slot.Name);
        Assert.Equal("Completed Show.nzb", slot.NzbName);
        Assert.Equal("tv", slot.Category);
        Assert.Equal(SabnzbdHistoryStatus.Completed, slot.Status);
        Assert.Equal(1_000_000, slot.Bytes);
    }

    [Fact]
    public void BuildHistorySlot_maps_failed_item()
    {
        var item = new HistoryItem(
            Guid.NewGuid(), new DownloadCompletion("Failed Show", MediaType.Show,
            500_000, DownloadStatus.Failed, null, "Download error", 0, 1719360000));

        var slot = SabnzbdResponseMapper.BuildHistorySlot(item, "/data/complete");

        Assert.Equal(SabnzbdHistoryStatus.Failed, slot.Status);
        Assert.Equal("Download error", slot.FailMessage);
        Assert.Null(slot.Storage);
    }

    [Fact]
    public void BuildHistorySlot_without_failure_message_yields_empty_fail_message()
    {
        var item = new HistoryItem(
            Guid.NewGuid(), new DownloadCompletion("Completed Show", MediaType.Show,
            1_000_000, DownloadStatus.Completed, "shows/show.mkv", null, 120, 1719360000));

        var slot = SabnzbdResponseMapper.BuildHistorySlot(item, "/data/complete");

        Assert.Equal(string.Empty, slot.FailMessage);
    }

    [Fact]
    public void FormatSpeed_returns_bytes_per_second_for_processing()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test", DownloadStatus.Processing,
            "ARD", false, 1_000_000, new DownloadProgress(500_000, 5_000_000, 1.0),
            60, MediaType.Show, DownloadPriority.Normal, DownloadPhase.VideoDownload, 1);

        var speed = SabnzbdResponseMapper.FormatSpeed(item);

        Assert.Equal("100000", speed);
    }

    [Fact]
    public void FormatSpeed_returns_zero_for_queued()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test", DownloadStatus.Queued,
            "ARD", false, 1_000_000, new DownloadProgress(0, 0, 0),
            0, MediaType.Show, DownloadPriority.Normal, DownloadPhase.Initialized, 0);

        Assert.Equal("0", SabnzbdResponseMapper.FormatSpeed(item));
    }

    [Fact]
    public void FormatTimeLeft_returns_formatted_time()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test", DownloadStatus.Processing,
            "ARD", false, 1_000_000, new DownloadProgress(500_000, 30_000_000, 1.0),
            120, MediaType.Show, DownloadPriority.Normal, DownloadPhase.VideoDownload, 1);

        var timeLeft = SabnzbdResponseMapper.FormatTimeLeft(item);

        Assert.Matches(@"\d{2}:\d{2}:\d{2}", timeLeft);
    }

    [Fact]
    public void FormatTimeLeft_returns_zeros_when_no_speed()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test", DownloadStatus.Queued,
            "ARD", false, 1_000_000, new DownloadProgress(0, 0, 0),
            0, MediaType.Show, DownloadPriority.Normal, DownloadPhase.Initialized, 0);

        Assert.Equal("00:00:00", SabnzbdResponseMapper.FormatTimeLeft(item));
    }

    [Fact]
    public void MapMediaTypeToCategory_maps_correctly()
    {
        Assert.Equal("movies", SabnzbdResponseMapper.MapMediaTypeToCategory(MediaType.Movie));
        Assert.Equal("tv", SabnzbdResponseMapper.MapMediaTypeToCategory(MediaType.Show));
    }

    [Fact]
    public void ParseMediaType_maps_correctly()
    {
        Assert.Equal(MediaType.Movie, SabnzbdResponseMapper.ParseMediaType("movie"));
        Assert.Equal(MediaType.Movie, SabnzbdResponseMapper.ParseMediaType("movies"));
        Assert.Equal(MediaType.Show, SabnzbdResponseMapper.ParseMediaType("tv"));
        Assert.Equal(MediaType.Show, SabnzbdResponseMapper.ParseMediaType("anything"));
    }

    [Fact]
    public void ParseMediaTypeNullable_maps_correctly()
    {
        Assert.Equal(MediaType.Movie, SabnzbdResponseMapper.ParseMediaTypeNullable("movie"));
        Assert.Equal(MediaType.Show, SabnzbdResponseMapper.ParseMediaTypeNullable("tv"));
        Assert.Null(SabnzbdResponseMapper.ParseMediaTypeNullable(null));
        Assert.Null(SabnzbdResponseMapper.ParseMediaTypeNullable(""));
        Assert.Null(SabnzbdResponseMapper.ParseMediaTypeNullable("unknown"));
    }

    [Fact]
    public void MapSabnzbdPriority_maps_correctly()
    {
        Assert.Equal(DownloadPriority.Low, SabnzbdResponseMapper.MapSabnzbdPriority("-1"));
        Assert.Equal(DownloadPriority.Normal, SabnzbdResponseMapper.MapSabnzbdPriority("0"));
        Assert.Equal(DownloadPriority.High, SabnzbdResponseMapper.MapSabnzbdPriority("1"));
        Assert.Equal(DownloadPriority.High, SabnzbdResponseMapper.MapSabnzbdPriority("2"));
        Assert.Equal(DownloadPriority.Normal, SabnzbdResponseMapper.MapSabnzbdPriority(null));
        Assert.Equal(DownloadPriority.Normal, SabnzbdResponseMapper.MapSabnzbdPriority("abc"));
    }

    [Theory]
    [InlineData(DownloadPriority.Low, SabnzbdPriority.Low)]
    [InlineData(DownloadPriority.Normal, SabnzbdPriority.Normal)]
    [InlineData(DownloadPriority.High, SabnzbdPriority.High)]
    public void BuildQueueSlot_maps_priority(DownloadPriority priority, SabnzbdPriority expected)
    {
        var slot = SabnzbdResponseMapper.BuildQueueSlot(QueueItemWith(DownloadStatus.Queued, priority), 0);

        Assert.Equal(expected, slot.Priority);
    }

    [Fact]
    public void BuildQueueSlot_throws_for_unexpected_priority()
    {
        var item = QueueItemWith(DownloadStatus.Queued, (DownloadPriority)99);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => SabnzbdResponseMapper.BuildQueueSlot(item, 0));

        Assert.Multiple(
            () => Assert.Equal("priority", ex.ParamName),
            () => Assert.Equal((DownloadPriority)99, ex.ActualValue));
    }

    [Theory]
    [InlineData(DownloadStatus.Completed)]
    [InlineData(DownloadStatus.Failed)]
    public void BuildQueueSlot_throws_for_finished_status(DownloadStatus status)
    {
        var item = QueueItemWith(status, DownloadPriority.Normal);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => SabnzbdResponseMapper.BuildQueueSlot(item, 0));

        Assert.Multiple(
            () => Assert.Equal("status", ex.ParamName),
            () => Assert.Equal(status, ex.ActualValue));
    }

    [Theory]
    [InlineData(DownloadStatus.Queued)]
    [InlineData(DownloadStatus.Processing)]
    public void BuildHistorySlot_throws_for_unfinished_status(DownloadStatus status)
    {
        var item = new HistoryItem(
            Guid.NewGuid(),
            new DownloadCompletion("Show", MediaType.Show, 0, status, null, null, 0, 0));

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => SabnzbdResponseMapper.BuildHistorySlot(item, "/data/complete"));

        Assert.Multiple(
            () => Assert.Equal("status", ex.ParamName),
            () => Assert.Equal(status, ex.ActualValue));
    }

    [Fact]
    public void BuildQueueSlot_mb_uses_invariant_culture_with_decimal()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test", DownloadStatus.Processing,
            "ARD", false, 1_572_864, new DownloadProgress(524_288, 5_000_000, 1.0),
            60, MediaType.Show, DownloadPriority.Normal, DownloadPhase.VideoDownload, 1);

        var slot = SabnzbdResponseMapper.BuildQueueSlot(item, 0);

        Assert.Equal("1.5", slot.Mb);
        Assert.Equal("1.0", slot.Mbleft);
    }

    private static QueueItem QueueItemWith(DownloadStatus status, DownloadPriority priority) =>
        new(Guid.NewGuid(), "Title", status,
            "ARD", false, 1_000, new DownloadProgress(0, 0, 0),
            0, MediaType.Show, priority, DownloadPhase.Initialized, 0);
}
