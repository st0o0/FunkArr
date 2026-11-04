namespace FunkArr.Api.Models.Downloads;

public sealed record DownloadSettingsResponse(
    int ConcurrentDownloads,
    DownloadTimeSlotResponse[] Schedule);

public sealed record DownloadTimeSlotResponse(TimeOnly Start, TimeOnly End);
