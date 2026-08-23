using FunkArr.DownloadClient;
using Newtonsoft.Json;

namespace FunkArr.Persistence;

public sealed class QueueJobEnqueuedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("url")] public string DownloadUrl { get; set; } = "";
    [JsonProperty("t")] public string Title { get; set; } = "";
    [JsonProperty("sub")] public string? SubtitleUrl { get; set; }
    [JsonProperty("ts")] public long EnqueuedAtUtcTicks { get; set; }
}

public sealed class QueueJobStartedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
}

public sealed class QueueJobFinishedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
    [JsonProperty("out")] public string Outcome { get; set; } = "";
}

public sealed class QueueJobRemovedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("nzo")] public string NzoId { get; set; } = "";
}

public static class QueueCoordinatorEventDtoMapping
{
    public static QueueJobEnqueuedDto ToDto(QueueCoordinatorEvents.JobEnqueued evt) => new()
    {
        NzoId = evt.NzoId,
        DownloadUrl = evt.DownloadUrl,
        Title = evt.Title,
        SubtitleUrl = evt.SubtitleUrl,
        EnqueuedAtUtcTicks = evt.EnqueuedAt.UtcTicks,
    };

    public static QueueCoordinatorEvents.JobEnqueued ToDomain(QueueJobEnqueuedDto dto) =>
        new(dto.NzoId, dto.DownloadUrl, dto.Title, dto.SubtitleUrl,
            new DateTimeOffset(dto.EnqueuedAtUtcTicks, TimeSpan.Zero));

    public static QueueJobStartedDto ToDto(QueueCoordinatorEvents.JobStarted evt) => new()
    {
        NzoId = evt.NzoId,
    };

    public static QueueCoordinatorEvents.JobStarted ToDomain(QueueJobStartedDto dto) =>
        new(dto.NzoId);

    public static QueueJobFinishedDto ToDto(QueueCoordinatorEvents.JobFinished evt) => new()
    {
        NzoId = evt.NzoId,
        Outcome = evt.Outcome,
    };

    public static QueueCoordinatorEvents.JobFinished ToDomain(QueueJobFinishedDto dto) =>
        new(dto.NzoId, dto.Outcome);

    public static QueueJobRemovedDto ToDto(QueueCoordinatorEvents.JobRemoved evt) => new()
    {
        NzoId = evt.NzoId,
    };

    public static QueueCoordinatorEvents.JobRemoved ToDomain(QueueJobRemovedDto dto) =>
        new(dto.NzoId);
}
