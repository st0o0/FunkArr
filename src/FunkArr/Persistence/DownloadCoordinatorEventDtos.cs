using FunkArr.DownloadClient;
using Newtonsoft.Json;

namespace FunkArr.Persistence;

public sealed class DcJobAcceptedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("url")] public string VideoUrl { get; set; } = "";
    [JsonProperty("sub")] public string? SubtitleUrl { get; set; }
    [JsonProperty("tmp")] public string TempPath { get; set; } = "";
    [JsonProperty("out")] public string OutputDir { get; set; } = "";
    [JsonProperty("t")] public string Title { get; set; } = "";
}

public sealed class DcStageEnteredDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("s")] public string Stage { get; set; } = "";
}

public sealed class DcJobCompletedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("out")] public string OutputPath { get; set; } = "";
}

public sealed class DcJobFailedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("fk")] public string FailureKind { get; set; } = "";
    [JsonProperty("err")] public string Reason { get; set; } = "";
}

public sealed class DcJobCancelledDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
}

public static class DownloadCoordinatorEventDtoMapping
{
    public static DcJobAcceptedDto ToDto(DownloadCoordinatorStageEvents.JobAccepted evt) => new()
    {
        NzoId = evt.NzoId,
        VideoUrl = evt.VideoUrl,
        SubtitleUrl = evt.SubtitleUrl,
        TempPath = evt.TempPath,
        OutputDir = evt.OutputDir,
        Title = evt.Title,
    };

    public static DownloadCoordinatorStageEvents.JobAccepted ToDomain(DcJobAcceptedDto dto) =>
        new(dto.NzoId, dto.VideoUrl, dto.SubtitleUrl, dto.TempPath, dto.OutputDir, dto.Title);

    public static DcStageEnteredDto ToDto(DownloadCoordinatorStageEvents.StageEntered evt) => new()
    {
        NzoId = evt.NzoId,
        Stage = evt.Stage,
    };

    public static DownloadCoordinatorStageEvents.StageEntered ToDomain(DcStageEnteredDto dto) =>
        new(dto.NzoId, dto.Stage);

    public static DcJobCompletedDto ToDto(DownloadCoordinatorStageEvents.JobCompleted evt) => new()
    {
        NzoId = evt.NzoId,
        OutputPath = evt.OutputPath,
    };

    public static DownloadCoordinatorStageEvents.JobCompleted ToDomain(DcJobCompletedDto dto) =>
        new(dto.NzoId, dto.OutputPath);

    public static DcJobFailedDto ToDto(DownloadCoordinatorStageEvents.JobFailed evt) => new()
    {
        NzoId = evt.NzoId,
        FailureKind = evt.FailureKind,
        Reason = evt.Reason,
    };

    public static DownloadCoordinatorStageEvents.JobFailed ToDomain(DcJobFailedDto dto) =>
        new(dto.NzoId, dto.FailureKind, dto.Reason);

    public static DcJobCancelledDto ToDto(DownloadCoordinatorStageEvents.JobCancelled evt) => new()
    {
        NzoId = evt.NzoId,
    };

    public static DownloadCoordinatorStageEvents.JobCancelled ToDomain(DcJobCancelledDto dto) =>
        new(dto.NzoId);
}
