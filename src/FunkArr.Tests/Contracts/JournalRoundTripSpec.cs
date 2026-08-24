using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Pipeline;
using FunkArr.DownloadClient.Queue;
using FunkArr.DownloadClient.Tracker;
using FunkArr.Persistence;
using FunkArr.RuleSet;
using Newtonsoft.Json;

namespace FunkArr.Tests.Contracts;

public sealed class JournalRoundTripSpec
{
    private static readonly JsonSerializerSettings Settings = new();

    [Fact(Timeout = 5000)]
    public Task QueueJobEnqueued_WireFormat()
    {
        var evt = new QueueActorEvents.JobEnqueued("abc123", "https://example.com/video.mp4", "Test Show", null, null, new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero));
        var journal = evt.ToJournal();
        var json = JsonConvert.SerializeObject(journal, Formatting.Indented, Settings);
        return Verify(json);
    }

    [Fact(Timeout = 5000)]
    public void QueueJobEnqueued_RoundTrip()
    {
        var evt = new QueueActorEvents.JobEnqueued("abc123", "https://example.com/video.mp4", "Test Show", "https://example.com/sub.vtt", "tv", new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero));
        var journal = evt.ToJournal();
        var json = JsonConvert.SerializeObject(journal, Settings);
        var deserialized = JsonConvert.DeserializeObject<QueueJobEnqueued>(json, Settings)!;
        var result = deserialized.ToDomain();

        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.DownloadUrl, result.DownloadUrl);
        Assert.Equal(evt.Title, result.Title);
        Assert.Equal(evt.SubtitleUrl, result.SubtitleUrl);
        Assert.Equal(evt.Category, result.Category);
        Assert.Equal(evt.EnqueuedAt, result.EnqueuedAt);
    }

    [Fact(Timeout = 5000)]
    public void QueueJobStarted_RoundTrip()
    {
        var evt = new QueueActorEvents.JobStarted("abc123");
        var result = Deserialize<QueueJobStarted>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
    }

    [Fact(Timeout = 5000)]
    public void QueueJobFinished_RoundTrip()
    {
        var evt = new QueueActorEvents.JobFinished("abc123", "success");
        var result = Deserialize<QueueJobFinished>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.Outcome, result.Outcome);
    }

    [Fact(Timeout = 5000)]
    public void QueueJobRemoved_RoundTrip()
    {
        var evt = new QueueActorEvents.JobRemoved("abc123");
        var result = Deserialize<QueueJobRemoved>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
    }

    [Fact(Timeout = 5000)]
    public Task DcJobAccepted_WireFormat()
    {
        var evt = new DownloadActorStageEvents.JobAccepted("nzo1", "https://example.com/v.mp4", "https://example.com/s.vtt", "My Show S01E01", "tv");
        var json = JsonConvert.SerializeObject(evt.ToJournal(), Formatting.Indented, Settings);
        return Verify(json);
    }

    [Fact(Timeout = 5000)]
    public void DcJobAccepted_RoundTrip()
    {
        var evt = new DownloadActorStageEvents.JobAccepted("nzo1", "https://example.com/v.mp4", "https://example.com/s.vtt", "My Show S01E01", "tv");
        var result = Deserialize<DcJobAccepted>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.VideoUrl, result.VideoUrl);
        Assert.Equal(evt.SubtitleUrl, result.SubtitleUrl);
        Assert.Equal(evt.Title, result.Title);
        Assert.Equal(evt.Category, result.Category);
    }

    [Fact(Timeout = 5000)]
    public void DcStageEntered_RoundTrip()
    {
        var evt = new DownloadActorStageEvents.StageEntered("nzo1", "Fetching");
        var result = Deserialize<DcStageEntered>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.Stage, result.Stage);
    }

    [Fact(Timeout = 5000)]
    public void DcJobCompleted_RoundTrip()
    {
        var evt = new DownloadActorStageEvents.JobCompleted("nzo1", "/media/complete/show.mkv");
        var result = Deserialize<DcJobCompleted>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.OutputPath, result.OutputPath);
    }

    [Fact(Timeout = 5000)]
    public void DcJobFailed_RoundTrip()
    {
        var evt = new DownloadActorStageEvents.JobFailed("nzo1", "Gone", "404 Not Found");
        var result = Deserialize<DcJobFailed>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.FailureKind, result.FailureKind);
        Assert.Equal(evt.Reason, result.Reason);
    }

    [Fact(Timeout = 5000)]
    public void DcJobCancelled_RoundTrip()
    {
        var evt = new DownloadActorStageEvents.JobCancelled("nzo1");
        var result = Deserialize<DcJobCancelled>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
    }

    [Fact(Timeout = 5000)]
    public Task RequestCreated_WireFormat()
    {
        var evt = new DownloadRequestActorEvents.RequestCreated("nzo1", "Show S01E02", "https://example.com/dl", null, new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
        var json = JsonConvert.SerializeObject(evt.ToJournal(), Formatting.Indented, Settings);
        return Verify(json);
    }

    [Fact(Timeout = 5000)]
    public void RequestCreated_RoundTrip()
    {
        var evt = new DownloadRequestActorEvents.RequestCreated("nzo1", "Show S01E02", "https://example.com/dl", "movies", new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
        var result = Deserialize<Persistence.RequestCreated>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.Title, result.Title);
        Assert.Equal(evt.DownloadUrl, result.DownloadUrl);
        Assert.Equal(evt.Category, result.Category);
        Assert.Equal(evt.EnqueuedAt, result.EnqueuedAt);
    }

    [Fact(Timeout = 5000)]
    public void RequestStatusChanged_RoundTrip()
    {
        var evt = new DownloadRequestActorEvents.StatusChanged("nzo1", "Downloading");
        var result = Deserialize<Persistence.RequestStatusChanged>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.Status, result.Status);
    }

    [Fact(Timeout = 5000)]
    public void RequestCompleted_RoundTrip()
    {
        var evt = new DownloadRequestActorEvents.Completed("nzo1", "/media/out.mkv", new DateTimeOffset(2026, 3, 1, 13, 0, 0, TimeSpan.Zero));
        var result = Deserialize<Persistence.RequestCompleted>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.OutputPath, result.OutputPath);
        Assert.Equal(evt.CompletedAt, result.CompletedAt);
    }

    [Fact(Timeout = 5000)]
    public void RequestFailed_RoundTrip()
    {
        var evt = new DownloadRequestActorEvents.Failed("nzo1", "Network error", new DateTimeOffset(2026, 3, 1, 13, 0, 0, TimeSpan.Zero));
        var result = Deserialize<Persistence.RequestFailed>(evt.ToJournal()).ToDomain();
        Assert.Equal(evt.NzoId, result.NzoId);
        Assert.Equal(evt.Error, result.Error);
        Assert.Equal(evt.CompletedAt, result.CompletedAt);
    }

    [Fact(Timeout = 5000)]
    public Task MatchRecorded_WireFormat()
    {
        var record = new MatchRecord
        {
            Id = "mr-001",
            Timestamp = new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero),
            SearchTopic = "tatort",
            TvdbId = 12345,
            Season = 2026,
            Episode = 10,
            Source = "mediathek",
            TotalResults = 5,
            Matched = [],
            Filtered = [],
            Unmatched = [],
        };
        var evt = new MatchQualityActor.MatchRecorded(record);
        var json = JsonConvert.SerializeObject(evt.ToJournal(), Formatting.Indented, Settings);
        return Verify(json);
    }

    [Fact(Timeout = 5000)]
    public void MatchRecorded_RoundTrip()
    {
        var record = new MatchRecord
        {
            Id = "mr-001",
            Timestamp = new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero),
            SearchTopic = "tatort",
            TvdbId = 12345,
            Season = 2026,
            Episode = 10,
            Source = "mediathek",
            TotalResults = 5,
            Matched = [],
            Filtered = [],
            Unmatched = [],
        };
        var evt = new MatchQualityActor.MatchRecorded(record);
        var journal = evt.ToJournal();
        var json = JsonConvert.SerializeObject(journal, Settings);
        var deserialized = JsonConvert.DeserializeObject<MatchRecordedJournal>(json, Settings)!;
        var result = deserialized.ToDomain();

        Assert.Equal(record.Id, result.Record.Id);
        Assert.Equal(record.SearchTopic, result.Record.SearchTopic);
        Assert.Equal(record.TvdbId, result.Record.TvdbId);
    }

    [Fact(Timeout = 5000)]
    public void MatchesExpired_RoundTrip()
    {
        var evt = new MatchQualityActor.MatchesExpired(new DateTimeOffset(2026, 5, 25, 0, 0, 0, TimeSpan.Zero));
        var journal = evt.ToJournal();
        var json = JsonConvert.SerializeObject(journal, Settings);
        var deserialized = JsonConvert.DeserializeObject<MatchesExpiredJournal>(json, Settings)!;
        var result = deserialized.ToDomain();

        Assert.Equal(evt.OlderThan, result.OlderThan);
    }

    [Fact(Timeout = 5000)]
    public void OldEvent_WithoutCategory_DeserializesAsNull_QueueJobEnqueued()
    {
        var json = """{"v":1,"nzo":"x","url":"http://test","t":"title","sub":null,"ts":639040698000000000}""";
        var deserialized = JsonConvert.DeserializeObject<QueueJobEnqueued>(json, Settings)!;
        var domain = deserialized.ToDomain();
        Assert.Null(domain.Category);
        Assert.Equal("x", domain.NzoId);
    }

    [Fact(Timeout = 5000)]
    public void OldEvent_WithoutCategory_DeserializesAsNull_DcJobAccepted()
    {
        var json = """{"v":1,"nzo":"x","url":"http://test","sub":null,"tmp":"/t","out":"/o","t":"title"}""";
        var deserialized = JsonConvert.DeserializeObject<DcJobAccepted>(json, Settings)!;
        var domain = deserialized.ToDomain();
        Assert.Null(domain.Category);
        Assert.Equal("x", domain.NzoId);
    }

    [Fact(Timeout = 5000)]
    public void OldEvent_WithoutCategory_DeserializesAsNull_RequestCreated()
    {
        var json = """{"v":1,"nzo":"x","t":"title","url":"http://test","ts":639040698000000000}""";
        var deserialized = JsonConvert.DeserializeObject<Persistence.RequestCreated>(json, Settings)!;
        var domain = deserialized.ToDomain();
        Assert.Null(domain.Category);
        Assert.Equal("x", domain.NzoId);
    }

    [Fact(Timeout = 5000)]
    public void UnknownFields_AreIgnored_QueueJobEnqueued()
    {
        var json = """{"v":1,"nzo":"x","url":"http://test","t":"title","ts":0,"unknownField":"value","extra":42}""";
        var deserialized = JsonConvert.DeserializeObject<QueueJobEnqueued>(json, Settings);
        Assert.NotNull(deserialized);
        Assert.Equal("x", deserialized!.NzoId);
    }

    [Fact(Timeout = 5000)]
    public void UnknownFields_AreIgnored_DcJobAccepted()
    {
        var json = """{"v":1,"nzo":"x","url":"http://test","tmp":"/t","out":"/o","t":"title","future":true}""";
        var deserialized = JsonConvert.DeserializeObject<DcJobAccepted>(json, Settings);
        Assert.NotNull(deserialized);
        Assert.Equal("x", deserialized!.NzoId);
    }

    [Fact(Timeout = 5000)]
    public void UnknownFields_AreIgnored_RequestCreated()
    {
        var json = """{"v":1,"nzo":"x","t":"title","url":"http://test","ts":0,"newProp":"test"}""";
        var deserialized = JsonConvert.DeserializeObject<Persistence.RequestCreated>(json, Settings);
        Assert.NotNull(deserialized);
        Assert.Equal("x", deserialized!.NzoId);
    }

    [Fact(Timeout = 5000)]
    public void UnknownFields_AreIgnored_MatchRecordedJournal()
    {
        var json = """{"v":1,"r":"{}","futureField":123}""";
        var deserialized = JsonConvert.DeserializeObject<MatchRecordedJournal>(json, Settings);
        Assert.NotNull(deserialized);
    }

    private static T Deserialize<T>(T journal) where T : class
    {
        var json = JsonConvert.SerializeObject(journal, Settings);
        return JsonConvert.DeserializeObject<T>(json, Settings)!;
    }
}
