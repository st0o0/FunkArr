using FunkArr.ArrApi.Sabnzbd;
using FunkArr.Messages;
using FunkArr.Messages.Download;

namespace FunkArr.ArrApi.Tests.Sabnzbd;

public sealed class SabnzbdResponseMapperTests
{
    [Fact]
    public void BuildQueueSlot_maps_processing_item()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test Download", DownloadStatus.Processing,
            "ARD", false, 1_048_576_000, 524_288_000, 30_000_000, 60, 1.0,
            MediaType.Show, DownloadPriority.Normal);

        var slot = SabnzbdResponseMapper.BuildQueueSlot(item, 0);

        Assert.Equal("Downloading", slot.Status);
        Assert.Equal("Test Download", slot.Filename);
        Assert.Equal("tv", slot.Cat);
        Assert.Equal(0, slot.Index);
        Assert.Equal("1000", slot.Mb);
        Assert.Equal("500", slot.Mbleft);
    }

    [Fact]
    public void BuildQueueSlot_maps_queued_item()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Queued", DownloadStatus.Queued,
            "ZDF", false, 500_000_000, 0, 0, 0, 0,
            MediaType.Movie, DownloadPriority.High);

        var slot = SabnzbdResponseMapper.BuildQueueSlot(item, 3);

        Assert.Equal("Queued", slot.Status);
        Assert.Equal(3, slot.Index);
        Assert.Equal("movie", slot.Cat);
    }

    [Fact]
    public void BuildHistorySlot_maps_completed_item()
    {
        var item = new HistoryItem(
            Guid.NewGuid(), "Completed Show", MediaType.Show,
            1_000_000, 120, "shows/show.mkv", DownloadStatus.Completed, "", 1719360000);

        var slot = SabnzbdResponseMapper.BuildHistorySlot(item, "/data/complete");

        Assert.Equal("Completed Show", slot.Name);
        Assert.Equal("Completed Show.nzb", slot.NzbName);
        Assert.Equal("tv", slot.Category);
        Assert.Equal("Completed", slot.Status);
        Assert.Equal(1_000_000, slot.Bytes);
    }

    [Fact]
    public void BuildHistorySlot_maps_failed_item()
    {
        var item = new HistoryItem(
            Guid.NewGuid(), "Failed Show", MediaType.Show,
            500_000, 0, "", DownloadStatus.Failed, "Download error", 1719360000);

        var slot = SabnzbdResponseMapper.BuildHistorySlot(item, "/data/complete");

        Assert.Equal("Failed", slot.Status);
        Assert.Equal("Download error", slot.FailMessage);
        Assert.Null(slot.Storage);
    }

    [Fact]
    public void FormatSpeed_returns_bytes_per_second_for_processing()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test", DownloadStatus.Processing,
            "ARD", false, 1_000_000, 500_000, 5_000_000, 60, 1.0,
            MediaType.Show, DownloadPriority.Normal);

        var speed = SabnzbdResponseMapper.FormatSpeed(item);

        Assert.Equal("100000", speed);
    }

    [Fact]
    public void FormatSpeed_returns_zero_for_queued()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test", DownloadStatus.Queued,
            "ARD", false, 1_000_000, 0, 0, 0, 0,
            MediaType.Show, DownloadPriority.Normal);

        Assert.Equal("0", SabnzbdResponseMapper.FormatSpeed(item));
    }

    [Fact]
    public void FormatTimeLeft_returns_formatted_time()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test", DownloadStatus.Processing,
            "ARD", false, 1_000_000, 500_000, 30_000_000, 120, 1.0,
            MediaType.Show, DownloadPriority.Normal);

        var timeLeft = SabnzbdResponseMapper.FormatTimeLeft(item);

        Assert.Matches(@"\d{2}:\d{2}:\d{2}", timeLeft);
    }

    [Fact]
    public void FormatTimeLeft_returns_zeros_when_no_speed()
    {
        var item = new QueueItem(
            Guid.NewGuid(), "Test", DownloadStatus.Queued,
            "ARD", false, 1_000_000, 0, 0, 0, 0,
            MediaType.Show, DownloadPriority.Normal);

        Assert.Equal("00:00:00", SabnzbdResponseMapper.FormatTimeLeft(item));
    }

    [Fact]
    public void MapMediaTypeToCategory_maps_correctly()
    {
        Assert.Equal("movie", SabnzbdResponseMapper.MapMediaTypeToCategory(MediaType.Movie));
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
}
