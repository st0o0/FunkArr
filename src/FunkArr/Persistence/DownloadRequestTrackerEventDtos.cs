using FunkArr.DownloadClient;
using Newtonsoft.Json;

namespace FunkArr.Persistence;

public sealed class RequestCreatedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("t")] public string Title { get; set; } = "";
    [JsonProperty("url")] public string DownloadUrl { get; set; } = "";
    [JsonProperty("ts")] public long EnqueuedAtUtcTicks { get; set; }
}

public sealed class RequestStatusChangedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("s")] public string Status { get; set; } = "";
}

public sealed class RequestCompletedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("out")] public string OutputPath { get; set; } = "";
    [JsonProperty("ts")] public long CompletedAtUtcTicks { get; set; }
}

public sealed class RequestFailedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("err")] public string Error { get; set; } = "";
    [JsonProperty("ts")] public long CompletedAtUtcTicks { get; set; }
}

public static class DownloadRequestTrackerEventDtoMapping
{
    public static RequestCreatedDto ToDto(DownloadRequestTrackerEvents.RequestCreated evt) => new()
    {
        NzoId = evt.NzoId,
        Title = evt.Title,
        DownloadUrl = evt.DownloadUrl,
        EnqueuedAtUtcTicks = evt.EnqueuedAt.UtcTicks,
    };

    public static DownloadRequestTrackerEvents.RequestCreated ToDomain(RequestCreatedDto dto) =>
        new(dto.NzoId, dto.Title, dto.DownloadUrl, new DateTimeOffset(dto.EnqueuedAtUtcTicks, TimeSpan.Zero));

    public static RequestStatusChangedDto ToDto(DownloadRequestTrackerEvents.StatusChanged evt) => new()
    {
        NzoId = evt.NzoId,
        Status = evt.Status,
    };

    public static DownloadRequestTrackerEvents.StatusChanged ToDomain(RequestStatusChangedDto dto) =>
        new(dto.NzoId, dto.Status);

    public static RequestCompletedDto ToDto(DownloadRequestTrackerEvents.Completed evt) => new()
    {
        NzoId = evt.NzoId,
        OutputPath = evt.OutputPath,
        CompletedAtUtcTicks = evt.CompletedAt.UtcTicks,
    };

    public static DownloadRequestTrackerEvents.Completed ToDomain(RequestCompletedDto dto) =>
        new(dto.NzoId, dto.OutputPath, new DateTimeOffset(dto.CompletedAtUtcTicks, TimeSpan.Zero));

    public static RequestFailedDto ToDto(DownloadRequestTrackerEvents.Failed evt) => new()
    {
        NzoId = evt.NzoId,
        Error = evt.Error,
        CompletedAtUtcTicks = evt.CompletedAt.UtcTicks,
    };

    public static DownloadRequestTrackerEvents.Failed ToDomain(RequestFailedDto dto) =>
        new(dto.NzoId, dto.Error, new DateTimeOffset(dto.CompletedAtUtcTicks, TimeSpan.Zero));
}
