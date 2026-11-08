using System.Globalization;
using FunkArr.ArrApi.Sabnzbd.Models;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using static FunkArr.Messages.Download.DownloadPhaseExtensions;

namespace FunkArr.ArrApi.Sabnzbd.Services;

internal static class SabnzbdResponseMapper
{
    internal static QueueSlot BuildQueueSlot(QueueItem item, int index) => new(
        NzoId: item.DownloadId.ToString(),
        Status: MapQueueStatus(item.Status),
        Index: index,
        Timeleft: FormatTimeLeft(item),
        Mb: (item.TotalBytes / 1_048_576.0).ToString("F1", CultureInfo.InvariantCulture),
        Filename: item.Title,
        Cat: MapMediaTypeToCategory(item.Category),
        Mbleft: ((item.TotalBytes - item.Progress.BytesDownloaded) / 1_048_576.0).ToString("F1", CultureInfo.InvariantCulture),
        Percentage: DerivePhase(item.Progress.BytesDownloaded, item.TotalBytes, item.Progress.CurrentTimeUs)
            .CalculatePercentage(item.Progress.BytesDownloaded, item.TotalBytes, item.Progress.CurrentTimeUs, item.TotalDuration)
            .ToString(),
        Priority: MapPriority(item.Priority),
        Speed: FormatSpeed(item));

    internal static HistorySlot BuildHistorySlot(HistoryItem item, string completePath) => new(
        NzoId: item.DownloadId.ToString(),
        Name: item.Completion.Title,
        NzbName: item.Completion.Title + ".nzb",
        Category: MapMediaTypeToCategory(item.Completion.Category),
        Bytes: item.Completion.Size,
        DownloadTime: item.Completion.DownloadTimeSeconds,
        Storage: !string.IsNullOrEmpty(item.Completion.RelativePath)
            ? Path.GetDirectoryName(Path.Join(completePath, item.Completion.RelativePath))
            : null,
        Status: MapHistoryStatus(item.Completion.Status),
        FailMessage: item.Completion.FailMessage ?? string.Empty,
        CompletedOn: item.Completion.CompletedAt);

    internal static string FormatSpeed(QueueItem item)
    {
        if (item.Status != DownloadStatus.Processing || item.Progress.CurrentTimeUs <= 0)
        {
            return "0";
        }

        var elapsedSeconds = item.Progress.CurrentTimeUs / 1_000_000.0;
        var bytesPerSecond = item.Progress.BytesDownloaded / elapsedSeconds;
        return ((long)bytesPerSecond).ToString();
    }

    internal static string FormatTimeLeft(QueueItem item)
    {
        if (item.TotalDuration <= 0 || item.Progress.Speed <= 0)
        {
            return "00:00:00";
        }

        var elapsedSeconds = item.Progress.CurrentTimeUs / 1_000_000.0;
        var remainingSeconds = (item.TotalDuration - elapsedSeconds) / item.Progress.Speed;
        if (remainingSeconds < 0)
        {
            remainingSeconds = 0;
        }

        var ts = TimeSpan.FromSeconds(remainingSeconds);
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    internal static MediaType ParseMediaType(string value) => value switch
    {
        "movie" or "movies" => MediaType.Movie,
        _ => MediaType.Show
    };

    internal static MediaType? ParseMediaTypeNullable(string? value) => value switch
    {
        null or "" => null,
        "movie" or "movies" => MediaType.Movie,
        "tv" or "show" => MediaType.Show,
        _ => null
    };

    internal static string MapMediaTypeToCategory(MediaType mediaType) => mediaType switch
    {
        MediaType.Movie => "movies",
        _ => "tv"
    };

    internal static DownloadPriority MapSabnzbdPriority(string? value)
    {
        if (!int.TryParse(value, out var intVal))
        {
            return DownloadPriority.Normal;
        }

        return intVal switch
        {
            -1 => DownloadPriority.Low,
            1 => DownloadPriority.High,
            2 => DownloadPriority.High,
            _ => DownloadPriority.Normal
        };
    }

    private static SabnzbdQueueStatus MapQueueStatus(DownloadStatus status) => status switch
    {
        DownloadStatus.Processing => SabnzbdQueueStatus.Downloading,
        DownloadStatus.Queued => SabnzbdQueueStatus.Queued,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static SabnzbdHistoryStatus MapHistoryStatus(DownloadStatus status) => status switch
    {
        DownloadStatus.Completed => SabnzbdHistoryStatus.Completed,
        DownloadStatus.Failed => SabnzbdHistoryStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static SabnzbdPriority MapPriority(DownloadPriority priority) => priority switch
    {
        DownloadPriority.Low => SabnzbdPriority.Low,
        DownloadPriority.Normal => SabnzbdPriority.Normal,
        DownloadPriority.High => SabnzbdPriority.High,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
    };
}
