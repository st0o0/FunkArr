using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Pipeline;
using Newtonsoft.Json;

namespace FunkArr.Persistence;

public sealed class DcJobAccepted
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("url")] public string VideoUrl { get; set; } = "";
    [JsonProperty("sub")] public string? SubtitleUrl { get; set; }
    [JsonProperty("tmp")] public string TempPath { get; set; } = "";
    [JsonProperty("out")] public string OutputDir { get; set; } = "";
    [JsonProperty("t")] public string Title { get; set; } = "";
    [JsonProperty("cat")] public string? Category { get; set; }
}

public sealed class DcStageEntered
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("s")] public string Stage { get; set; } = "";
}

public sealed class DcJobCompleted
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("out")] public string OutputPath { get; set; } = "";
}

public sealed class DcJobFailed
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("fk")] public string FailureKind { get; set; } = "";
    [JsonProperty("err")] public string Reason { get; set; } = "";
}

public sealed class DcJobCancelled
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
}

public static class DownloadActorJournalExtensions
{
    public static DcJobAccepted ToJournal(this DownloadActorStageEvents.JobAccepted evt) => new()
    {
        NzoId = evt.NzoId,
        VideoUrl = evt.VideoUrl,
        SubtitleUrl = evt.SubtitleUrl,
        TempPath = "",
        OutputDir = "",
        Title = evt.Title,
        Category = evt.Category,
    };

    public static DownloadActorStageEvents.JobAccepted ToDomain(this DcJobAccepted j) =>
        new(j.NzoId, j.VideoUrl, j.SubtitleUrl, j.Title, j.Category);

    public static DcStageEntered ToJournal(this DownloadActorStageEvents.StageEntered evt) => new()
    {
        NzoId = evt.NzoId,
        Stage = evt.Stage,
    };

    public static DownloadActorStageEvents.StageEntered ToDomain(this DcStageEntered j) =>
        new(j.NzoId, j.Stage);

    public static DcJobCompleted ToJournal(this DownloadActorStageEvents.JobCompleted evt) => new()
    {
        NzoId = evt.NzoId,
        OutputPath = evt.OutputPath,
    };

    public static DownloadActorStageEvents.JobCompleted ToDomain(this DcJobCompleted j) =>
        new(j.NzoId, j.OutputPath);

    public static DcJobFailed ToJournal(this DownloadActorStageEvents.JobFailed evt) => new()
    {
        NzoId = evt.NzoId,
        FailureKind = evt.FailureKind,
        Reason = evt.Reason,
    };

    public static DownloadActorStageEvents.JobFailed ToDomain(this DcJobFailed j) =>
        new(j.NzoId, j.FailureKind, j.Reason);

    public static DcJobCancelled ToJournal(this DownloadActorStageEvents.JobCancelled evt) => new()
    {
        NzoId = evt.NzoId,
    };

    public static DownloadActorStageEvents.JobCancelled ToDomain(this DcJobCancelled j) =>
        new(j.NzoId);
}
