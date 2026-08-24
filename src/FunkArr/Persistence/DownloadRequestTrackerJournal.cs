using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Tracker;
using Newtonsoft.Json;

namespace FunkArr.Persistence;

public sealed class RequestCreated
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("t")] public string Title { get; set; } = "";
    [JsonProperty("url")] public string DownloadUrl { get; set; } = "";
    [JsonProperty("cat")] public string? Category { get; set; }
    [JsonProperty("ts")] public long EnqueuedAtUtcTicks { get; set; }
}

public sealed class RequestStatusChanged
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("s")] public string Status { get; set; } = "";
}

public sealed class RequestCompleted
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("out")] public string OutputPath { get; set; } = "";
    [JsonProperty("ts")] public long CompletedAtUtcTicks { get; set; }
}

public sealed class RequestFailed
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("err")] public string Error { get; set; } = "";
    [JsonProperty("ts")] public long CompletedAtUtcTicks { get; set; }
}

public static class DownloadRequestActorJournalExtensions
{
    public static RequestCreated ToJournal(this DownloadRequestActorEvents.RequestCreated evt) => new()
    {
        NzoId = evt.NzoId,
        Title = evt.Title,
        DownloadUrl = evt.DownloadUrl,
        Category = evt.Category,
        EnqueuedAtUtcTicks = evt.EnqueuedAt.UtcTicks,
    };

    public static DownloadRequestActorEvents.RequestCreated ToDomain(this RequestCreated j) =>
        new(j.NzoId, j.Title, j.DownloadUrl, j.Category, new DateTimeOffset(j.EnqueuedAtUtcTicks, TimeSpan.Zero));

    public static RequestStatusChanged ToJournal(this DownloadRequestActorEvents.StatusChanged evt) => new()
    {
        NzoId = evt.NzoId,
        Status = evt.Status,
    };

    public static DownloadRequestActorEvents.StatusChanged ToDomain(this RequestStatusChanged j) =>
        new(j.NzoId, j.Status);

    public static RequestCompleted ToJournal(this DownloadRequestActorEvents.Completed evt) => new()
    {
        NzoId = evt.NzoId,
        OutputPath = evt.OutputPath,
        CompletedAtUtcTicks = evt.CompletedAt.UtcTicks,
    };

    public static DownloadRequestActorEvents.Completed ToDomain(this RequestCompleted j) =>
        new(j.NzoId, j.OutputPath, new DateTimeOffset(j.CompletedAtUtcTicks, TimeSpan.Zero));

    public static RequestFailed ToJournal(this DownloadRequestActorEvents.Failed evt) => new()
    {
        NzoId = evt.NzoId,
        Error = evt.Error,
        CompletedAtUtcTicks = evt.CompletedAt.UtcTicks,
    };

    public static DownloadRequestActorEvents.Failed ToDomain(this RequestFailed j) =>
        new(j.NzoId, j.Error, new DateTimeOffset(j.CompletedAtUtcTicks, TimeSpan.Zero));
}
