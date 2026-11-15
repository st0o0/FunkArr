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

    [Fact]
    public Task DownloadMoved_shape()
    {
        var evt = new DownloadMoved(_testId, 3);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadMoved_roundtrip()
    {
        var original = new DownloadMoved(_testId, 3);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadMoved>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
        Assert.Equal(original.Position, result.Position);
    }

    [Fact]
    public Task DownloadSwapped_shape()
    {
        var id2 = new Guid("11223344-5566-7788-9900-aabbccddeeff");
        var evt = new DownloadSwapped(_testId, id2);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadSwapped_roundtrip()
    {
        var id2 = new Guid("11223344-5566-7788-9900-aabbccddeeff");
        var original = new DownloadSwapped(_testId, id2);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadSwapped>(json)!;
        Assert.Equal(original.DownloadId1, result.DownloadId1);
        Assert.Equal(original.DownloadId2, result.DownloadId2);
    }

    [Fact]
    public Task DownloadPriorityChanged_shape()
    {
        var evt = new DownloadPriorityChanged(_testId, PersistedDownloadPriority.High);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadPriorityChanged_roundtrip()
    {
        var original = new DownloadPriorityChanged(_testId, PersistedDownloadPriority.High);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadPriorityChanged>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
        Assert.Equal(original.Priority, result.Priority);
    }

    [Fact]
    public Task DownloadsPaused_shape()
    {
        var evt = new DownloadsPaused();
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadsPaused_roundtrip()
    {
        var original = new DownloadsPaused();
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadsPaused>(json)!;
        Assert.NotNull(result);
    }

    [Fact]
    public Task DownloadsResumed_shape()
    {
        var evt = new DownloadsResumed();
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadsResumed_roundtrip()
    {
        var original = new DownloadsResumed();
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadsResumed>(json)!;
        Assert.NotNull(result);
    }

    [Fact]
    public Task DownloadAttemptStarted_shape()
    {
        var evt = new DownloadAttemptStarted(_testId, 2);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void DownloadAttemptStarted_roundtrip()
    {
        var original = new DownloadAttemptStarted(_testId, 2);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<DownloadAttemptStarted>(json)!;
        Assert.Equal(original.DownloadId, result.DownloadId);
        Assert.Equal(original.Attempt, result.Attempt);
    }

    [Fact]
    public Task PersistedDownloadManagerState_shape()
    {
        var state = new PersistedDownloadManagerState(
            [new PersistedQueueEntry(_testId, PersistedDownloadPriority.Normal, PersistedMediaType.Show)],
            [new PersistedDispatchedEntry(new Guid("11223344-5566-7788-9900-aabbccddeeff"), PersistedDownloadPriority.High, PersistedMediaType.Movie)],
            false);
        var json = JsonConvert.SerializeObject(state, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void PersistedDownloadManagerState_roundtrip()
    {
        var original = new PersistedDownloadManagerState(
            [new PersistedQueueEntry(_testId, PersistedDownloadPriority.Normal)],
            [new PersistedDispatchedEntry(new Guid("11223344-5566-7788-9900-aabbccddeeff"), PersistedDownloadPriority.High)],
            true);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<PersistedDownloadManagerState>(json)!;
        Assert.Single(result.Queued);
        Assert.Equal(_testId, result.Queued[0].DownloadId);
        Assert.Equal(PersistedDownloadPriority.Normal, result.Queued[0].Priority);
        Assert.Single(result.Dispatched);
        Assert.True(result.Paused);
    }

    [Fact]
    public Task HistoryTrimmed_shape()
    {
        var evt = new HistoryTrimmed(5);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void HistoryTrimmed_roundtrip()
    {
        var original = new HistoryTrimmed(5);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<HistoryTrimmed>(json)!;
        Assert.Equal(original.Count, result.Count);
    }
}
