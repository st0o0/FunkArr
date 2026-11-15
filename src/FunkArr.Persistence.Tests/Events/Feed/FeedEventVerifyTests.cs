using FunkArr.Persistence.Events.Feed;
using Newtonsoft.Json;

namespace FunkArr.Persistence.Tests.Events.Feed;

public sealed class FeedEventVerifyTests
{
    private static readonly DateTimeOffset _testTimestamp =
        new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public Task FeedUnchangedRun_shape()
    {
        var evt = new FeedUnchangedRun(_testTimestamp);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void FeedUnchangedRun_roundtrip()
    {
        var original = new FeedUnchangedRun(_testTimestamp);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<FeedUnchangedRun>(json)!;
        Assert.Equal(original.Timestamp, result.Timestamp);
    }

    [Fact]
    public Task FeedCursorAdvanced_shape()
    {
        var evt = new FeedCursorAdvanced(1700000000, _testTimestamp, 42, 38, 4, false);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void FeedCursorAdvanced_roundtrip()
    {
        var original = new FeedCursorAdvanced(1700000000, _testTimestamp, 42, 38, 4, false);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<FeedCursorAdvanced>(json)!;
        Assert.Equal(original.Cursor, result.Cursor);
        Assert.Equal(original.CompletedAt, result.CompletedAt);
        Assert.Equal(original.NewItems, result.NewItems);
        Assert.Equal(original.Published, result.Published);
        Assert.Equal(original.Unmatched, result.Unmatched);
        Assert.Equal(original.Gap, result.Gap);
    }

    [Fact]
    public Task FeedCursorAdvanced_with_gap_shape()
    {
        var evt = new FeedCursorAdvanced(1700000000, _testTimestamp, 0, 0, 0, true);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public Task FeedRunFailed_shape()
    {
        var evt = new FeedRunFailed(_testTimestamp, "Connection timeout", 3);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void FeedRunFailed_roundtrip()
    {
        var original = new FeedRunFailed(_testTimestamp, "Connection timeout", 3);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<FeedRunFailed>(json)!;
        Assert.Equal(original.FailedAt, result.FailedAt);
        Assert.Equal(original.Reason, result.Reason);
        Assert.Equal(original.ConsecutiveFailures, result.ConsecutiveFailures);
    }

    [Fact]
    public Task FeedEntriesAdded_shape()
    {
        var entry = new PersistedFeedEntry(
            "stable-123", _testTimestamp, PersistedMediaType.Show,
            "Tatort: Sturm", "ARD", "Tatort",
            "https://example.com/video.mp4", 5400, 1073741824, 720,
            _testTimestamp.AddDays(-1), 0.95, "https://example.com/sub.vtt",
            83214, "tt1234567", 12345, "2024", "5");
        var evt = new FeedEntriesAdded([entry]);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void FeedEntriesAdded_roundtrip()
    {
        var entry = new PersistedFeedEntry(
            "stable-123", _testTimestamp, PersistedMediaType.Show,
            "Tatort: Sturm", "ARD", "Tatort",
            "https://example.com/video.mp4", 5400, 1073741824, 720,
            _testTimestamp.AddDays(-1), 0.95, "https://example.com/sub.vtt",
            83214, "tt1234567", 12345, "2024", "5");
        var original = new FeedEntriesAdded([entry]);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<FeedEntriesAdded>(json)!;
        Assert.Single(result.Entries);
        Assert.Equal("stable-123", result.Entries[0].StableId);
        Assert.Equal("Tatort: Sturm", result.Entries[0].Title);
        Assert.Equal(PersistedMediaType.Show, result.Entries[0].MediaType);
        Assert.Equal(83214, result.Entries[0].TvdbId);
    }

    [Fact]
    public Task FeedEntriesExpired_shape()
    {
        var evt = new FeedEntriesExpired(["stable-001", "stable-002", "stable-003"]);
        var json = JsonConvert.SerializeObject(evt, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void FeedEntriesExpired_roundtrip()
    {
        var original = new FeedEntriesExpired(["stable-001", "stable-002"]);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<FeedEntriesExpired>(json)!;
        Assert.Equal(2, result.StableIds.Length);
        Assert.Equal("stable-001", result.StableIds[0]);
        Assert.Equal("stable-002", result.StableIds[1]);
    }

    [Fact]
    public Task PersistedFeedCrawlState_shape()
    {
        var state = new PersistedFeedCrawlState(
            1700000000, _testTimestamp, PersistedCrawlOutcome.Completed,
            null, 42, 38, 4, false, 0);
        var json = JsonConvert.SerializeObject(state, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void PersistedFeedCrawlState_roundtrip()
    {
        var original = new PersistedFeedCrawlState(
            1700000000, _testTimestamp, PersistedCrawlOutcome.Failed,
            "Connection timeout", 0, 0, 0, false, 3);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<PersistedFeedCrawlState>(json)!;
        Assert.Equal(original.Cursor, result.Cursor);
        Assert.Equal(original.LastRunAt, result.LastRunAt);
        Assert.Equal(original.LastOutcome, result.LastOutcome);
        Assert.Equal(original.LastError, result.LastError);
        Assert.Equal(original.ConsecutiveFailures, result.ConsecutiveFailures);
    }

    [Fact]
    public Task PersistedFeedStoreState_shape()
    {
        var entry = new PersistedFeedEntry(
            "stable-789", _testTimestamp, PersistedMediaType.Movie,
            "Film", "ZDF", "Spielfilm",
            "https://example.com/movie.mp4", 7200, 2_000_000_000, 1080,
            null, 0.5, null, null, null, null, null, null);
        var state = new PersistedFeedStoreState([entry]);
        var json = JsonConvert.SerializeObject(state, Formatting.Indented);
        return Verify(json);
    }

    [Fact]
    public void PersistedFeedStoreState_roundtrip()
    {
        var entry = new PersistedFeedEntry(
            "stable-789", _testTimestamp, PersistedMediaType.Movie,
            "Film", "ZDF", "Spielfilm",
            "https://example.com/movie.mp4", 7200, 2_000_000_000, 1080,
            null, 0.5, null, null, null, null, null, null);
        var original = new PersistedFeedStoreState([entry]);
        var json = JsonConvert.SerializeObject(original);
        var result = JsonConvert.DeserializeObject<PersistedFeedStoreState>(json)!;
        Assert.Single(result.Entries);
        Assert.Equal("stable-789", result.Entries[0].StableId);
        Assert.Equal(PersistedMediaType.Movie, result.Entries[0].MediaType);
        Assert.Null(result.Entries[0].TvdbId);
    }
}
