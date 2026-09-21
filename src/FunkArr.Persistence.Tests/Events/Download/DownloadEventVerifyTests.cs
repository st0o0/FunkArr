using FunkArr.Persistence.Events.Download;
using Newtonsoft.Json;

namespace FunkArr.Persistence.Tests.Events.Download;

public sealed class DownloadEventVerifyTests
{
    private static readonly Guid _testId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    [Fact]
    public Task DownloadEnqueued_shape()
    {
        var evt = new DownloadEnqueued(_testId);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadEnqueued_roundtrip()
    {
        var original = new DownloadEnqueued(_testId);
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
        var evt = new DownloadInitialized(
            _testId, "Tatort: Der letzte Schrei", "https://example.com/video.mp4",
            "https://example.com/sub.vtt", "ARD", 5400, 1073741824,
            PersistedMediaType.Show);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadInitialized_roundtrip()
    {
        var original = new DownloadInitialized(
            _testId, "Tatort: Der letzte Schrei", "https://example.com/video.mp4",
            "https://example.com/sub.vtt", "ARD", 5400, 1073741824,
            PersistedMediaType.Show);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadInitialized>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
        Assert.Equal(original.Title, result.Title);
        Assert.Equal(original.VideoUrl, result.VideoUrl);
        Assert.Equal(original.SubtitleUrl, result.SubtitleUrl);
        Assert.Equal(original.Channel, result.Channel);
        Assert.Equal(original.Duration, result.Duration);
        Assert.Equal(original.Size, result.Size);
        Assert.Equal(original.Category, result.Category);
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
    public Task HistoryRecorded_shape()
    {
        var evt = new HistoryRecorded(
            _testId, "Tatort: Der letzte Schrei", PersistedMediaType.Show,
            1073741824, 2, "/downloads/tatort.mkv", null, 120, 1700000000);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void HistoryRecorded_roundtrip()
    {
        var original = new HistoryRecorded(
            _testId, "Tatort: Der letzte Schrei", PersistedMediaType.Show,
            1073741824, 2, "/downloads/tatort.mkv", null, 120, 1700000000);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<HistoryRecorded>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
        Assert.Equal(original.Title, result.Title);
        Assert.Equal(original.Category, result.Category);
        Assert.Equal(original.Size, result.Size);
        Assert.Equal(original.Status, result.Status);
        Assert.Equal(original.RelativePath, result.RelativePath);
        Assert.Null(result.FailMessage);
        Assert.Equal(original.DownloadTimeSeconds, result.DownloadTimeSeconds);
        Assert.Equal(original.CompletedAt, result.CompletedAt);
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
