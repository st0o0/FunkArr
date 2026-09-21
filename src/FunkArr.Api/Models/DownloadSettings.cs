namespace FunkArr.Api.Models;

public sealed record DownloadSettingsResponse(
    int ConcurrentDownloads,
    DownloadTimeSlotResponse[] Schedule);

public sealed record DownloadTimeSlotResponse(string Start, string End);
