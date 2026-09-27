using FunkArr.Persistence.Events.Download;
using FunkArr.Persistence.Events.Shared;
using Newtonsoft.Json;

namespace FunkArr.Persistence.Tests.Events.Download;

public sealed class DownloadEventVerifyTests
{
    private static readonly Guid _testId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    [Fact]
    public Task DownloadEnqueued_shape()
    {
        var evt = new DownloadEnqueued(_testId, PersistedDownloadPriority.Normal);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadEnqueued_roundtrip()
    {
        var original = new DownloadEnqueued(_testId, PersistedDownloadPriority.Normal);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadEnqueued>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
    }

    [Fact]
    public Task DownloadDequeued_shape()
    {
        var evt = new DownloadDequeued(_testId);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadDequeued_roundtrip()
    {
        var original = new DownloadDequeued(_testId);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadDequeued>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
    }

    [Fact]
    public Task DownloadDispatched_shape()
    {
        var evt = new DownloadDispatched(_testId);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadDispatched_roundtrip()
    {
        var original = new DownloadDispatched(_testId);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadDispatched>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
    }

    [Fact]
    public Task DownloadStarted_shape()
    {
        var evt = new DownloadStarted(_testId);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadStarted_roundtrip()
    {
        var original = new DownloadStarted(_testId);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadStarted>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
    }

    [Fact]
    public Task DownloadInitialized_shape()
    {
        var media = new PersistedDownloadMedia(
            "Tatort: Der letzte Schrei", "https://example.com/video.mp4",
            "https://example.com/sub.vtt", "ARD", 5400, 1073741824,
            PersistedMediaType.Show);
        var evt = new DownloadInitialized(_testId, media);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadInitialized_roundtrip()
    {
        var media = new PersistedDownloadMedia(
            "Tatort: Der letzte Schrei", "https://example.com/video.mp4",
            "https://example.com/sub.vtt", "ARD", 5400, 1073741824,
            PersistedMediaType.Show);
        var original = new DownloadInitialized(_testId, media);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadInitialized>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
        Assert.Equal(original.Media.Title, result.Media.Title);
        Assert.Equal(original.Media.VideoUrl, result.Media.VideoUrl);
        Assert.Equal(original.Media.SubtitleUrl, result.Media.SubtitleUrl);
        Assert.Equal(original.Media.Channel, result.Media.Channel);
        Assert.Equal(original.Media.Duration, result.Media.Duration);
        Assert.Equal(original.Media.Size, result.Media.Size);
        Assert.Equal(original.Media.Category, result.Media.Category);
    }

    [Fact]
    public Task DownloadSucceeded_shape()
    {
        var evt = new DownloadSucceeded(_testId, 120, 1700000000);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadSucceeded_roundtrip()
    {
        var original = new DownloadSucceeded(_testId, 120, 1700000000);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadSucceeded>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
        Assert.Equal(original.DownloadTimeSeconds, result.DownloadTimeSeconds);
        Assert.Equal(original.CompletedAt, result.CompletedAt);
    }

    [Fact]
    public Task DownloadFaulted_shape()
    {
        var evt = new DownloadFaulted(_testId, "Connection timed out");
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadFaulted_roundtrip()
    {
        var original = new DownloadFaulted(_testId, "Connection timed out");
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadFaulted>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
        Assert.Equal(original.Reason, result.Reason);
    }

    [Fact]
    public Task DownloadHistoryRecorded_shape()
    {
        var completion = new PersistedDownloadCompletion(
            "Tatort: Der letzte Schrei", PersistedMediaType.Show,
            1073741824, PersistedDownloadStatus.Completed, "/downloads/tatort.mkv", null, 120, 1700000000);
        var evt = new DownloadHistoryRecorded(_testId, completion);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadHistoryRecorded_roundtrip()
    {
        var completion = new PersistedDownloadCompletion(
            "Tatort: Der letzte Schrei", PersistedMediaType.Show,
            1073741824, PersistedDownloadStatus.Completed, "/downloads/tatort.mkv", null, 120, 1700000000);
        var original = new DownloadHistoryRecorded(_testId, completion);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadHistoryRecorded>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
        Assert.Equal(original.Completion.Title, result.Completion.Title);
        Assert.Equal(original.Completion.Category, result.Completion.Category);
        Assert.Equal(original.Completion.Size, result.Completion.Size);
        Assert.Equal(original.Completion.Status, result.Completion.Status);
        Assert.Equal(original.Completion.RelativePath, result.Completion.RelativePath);
        Assert.Null(result.Completion.FailMessage);
        Assert.Equal(original.Completion.DownloadTimeSeconds, result.Completion.DownloadTimeSeconds);
        Assert.Equal(original.Completion.CompletedAt, result.Completion.CompletedAt);
    }

    [Fact]
    public Task HistoryRemoved_shape()
    {
        var evt = new HistoryRemoved(_testId);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void HistoryRemoved_roundtrip()
    {
        var original = new HistoryRemoved(_testId);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<HistoryRemoved>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
    }
}
