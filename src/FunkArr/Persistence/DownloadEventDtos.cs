using FunkArr.DownloadClient;
using Newtonsoft.Json;

namespace FunkArr.Persistence;

public sealed class DownloadEnqueuedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("url")] public string DownloadUrl { get; set; } = "";
    [JsonProperty("t")] public string Title { get; set; } = "";
    [JsonProperty("sub")] public string? SubtitleUrl { get; set; }
    [JsonProperty("ts")] public long EnqueuedAtUtcTicks { get; set; }
}

public sealed class DownloadStartedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
}

public sealed class DownloadCompletedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("path")] public string TempFilePath { get; set; } = "";
    [JsonProperty("sub")] public string? TempSubtitlePath { get; set; }
}

public sealed class DownloadFailedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("err")] public string Error { get; set; } = "";
}

public sealed class MuxingStartedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
}

public sealed class MuxingCompletedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("out")] public string OutputPath { get; set; } = "";
}

public sealed class MuxingFailedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("err")] public string Error { get; set; } = "";
}

public static class DownloadEventDtoMapping
{
    public static DownloadEnqueuedDto ToDto(DownloadEvents.DownloadEnqueued evt) => new()
    {
        NzoId = evt.NzoId,
        DownloadUrl = evt.DownloadUrl,
        Title = evt.Title,
        SubtitleUrl = evt.SubtitleUrl,
        EnqueuedAtUtcTicks = evt.EnqueuedAt.UtcTicks,
    };

    public static DownloadEvents.DownloadEnqueued ToDomain(DownloadEnqueuedDto dto) =>
        new(dto.NzoId, dto.DownloadUrl, dto.Title, dto.SubtitleUrl,
            new DateTimeOffset(dto.EnqueuedAtUtcTicks, TimeSpan.Zero));

    public static DownloadStartedDto ToDto(DownloadEvents.DownloadStarted evt) => new()
    {
        NzoId = evt.NzoId,
    };

    public static DownloadEvents.DownloadStarted ToDomain(DownloadStartedDto dto) =>
        new(dto.NzoId);

    public static DownloadCompletedDto ToDto(DownloadEvents.DownloadCompleted evt) => new()
    {
        NzoId = evt.NzoId,
        TempFilePath = evt.TempFilePath,
        TempSubtitlePath = evt.TempSubtitlePath,
    };

    public static DownloadEvents.DownloadCompleted ToDomain(DownloadCompletedDto dto) =>
        new(dto.NzoId, dto.TempFilePath, dto.TempSubtitlePath);

    public static DownloadFailedDto ToDto(DownloadEvents.DownloadFailed evt) => new()
    {
        NzoId = evt.NzoId,
        Error = evt.Error,
    };

    public static DownloadEvents.DownloadFailed ToDomain(DownloadFailedDto dto) =>
        new(dto.NzoId, dto.Error);

    public static MuxingStartedDto ToDto(DownloadEvents.MuxingStarted evt) => new()
    {
        NzoId = evt.NzoId,
    };

    public static DownloadEvents.MuxingStarted ToDomain(MuxingStartedDto dto) =>
        new(dto.NzoId);

    public static MuxingCompletedDto ToDto(DownloadEvents.MuxingCompleted evt) => new()
    {
        NzoId = evt.NzoId,
        OutputPath = evt.OutputPath,
    };

    public static DownloadEvents.MuxingCompleted ToDomain(MuxingCompletedDto dto) =>
        new(dto.NzoId, dto.OutputPath);

    public static MuxingFailedDto ToDto(DownloadEvents.MuxingFailed evt) => new()
    {
        NzoId = evt.NzoId,
        Error = evt.Error,
    };

    public static DownloadEvents.MuxingFailed ToDomain(MuxingFailedDto dto) =>
        new(dto.NzoId, dto.Error);
}
