using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Queue;
using Newtonsoft.Json;

namespace FunkArr.Persistence;

public sealed class QueueJobEnqueued
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("url")] public string DownloadUrl { get; set; } = "";
    [JsonProperty("t")] public string Title { get; set; } = "";
    [JsonProperty("sub")] public string? SubtitleUrl { get; set; }
    [JsonProperty("cat")] public string? Category { get; set; }
    [JsonProperty("ts")] public long EnqueuedAtUtcTicks { get; set; }
}

public sealed class QueueJobStarted
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
}

public sealed class QueueJobFinished
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("out")] public string Outcome { get; set; } = "";
}

public sealed class QueueJobRemoved
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
}

public static class QueueActorJournalExtensions
{
    public static QueueJobEnqueued ToJournal(this QueueActorEvents.JobEnqueued evt) => new()
    {
        NzoId = evt.NzoId,
        DownloadUrl = evt.DownloadUrl,
        Title = evt.Title,
        SubtitleUrl = evt.SubtitleUrl,
        Category = evt.Category,
        EnqueuedAtUtcTicks = evt.EnqueuedAt.UtcTicks,
    };

    public static QueueActorEvents.JobEnqueued ToDomain(this QueueJobEnqueued j) =>
        new(j.NzoId, j.DownloadUrl, j.Title, j.SubtitleUrl, j.Category,
            new DateTimeOffset(j.EnqueuedAtUtcTicks, TimeSpan.Zero));

    public static QueueJobStarted ToJournal(this QueueActorEvents.JobStarted evt) => new()
    {
        NzoId = evt.NzoId,
    };

    public static QueueActorEvents.JobStarted ToDomain(this QueueJobStarted j) =>
        new(j.NzoId);

    public static QueueJobFinished ToJournal(this QueueActorEvents.JobFinished evt) => new()
    {
        NzoId = evt.NzoId,
        Outcome = evt.Outcome,
    };

    public static QueueActorEvents.JobFinished ToDomain(this QueueJobFinished j) =>
        new(j.NzoId, j.Outcome);

    public static QueueJobRemoved ToJournal(this QueueActorEvents.JobRemoved evt) => new()
    {
        NzoId = evt.NzoId,
    };

    public static QueueActorEvents.JobRemoved ToDomain(this QueueJobRemoved j) =>
        new(j.NzoId);
}
