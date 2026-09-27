using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Shared;

namespace FunkArr.IntegrationTests;

internal static class TestData
{
    public static QueueItem QueueItem(
        string title,
        DownloadStatus status = DownloadStatus.Queued,
        long totalBytes = 500_000_000,
        long bytesDownloaded = 0,
        int totalDuration = 3600,
        MediaType category = MediaType.Show,
        DownloadPriority priority = DownloadPriority.Normal,
        string channel = "ARD",
        bool hasSubtitles = false)
    {
        var progress = new DownloadProgress(bytesDownloaded, 0, bytesDownloaded > 0 ? 1.5 : 0);
        var phase = bytesDownloaded > 0 ? DownloadPhase.VideoDownload : DownloadPhase.Initialized;
        return new QueueItem(
            Guid.NewGuid(), title, status, channel, hasSubtitles,
            totalBytes, progress, totalDuration, category, priority, phase, 1);
    }

    public static QueueItem ProcessingItem(
        string title = "Tatort.S01E05.720p.WEB-DL",
        long totalBytes = 500_000_000,
        int percentComplete = 50)
    {
        var downloaded = totalBytes * percentComplete / 100;
        return QueueItem(title, DownloadStatus.Processing, totalBytes,
            bytesDownloaded: downloaded);
    }

    public static HistoryItem CompletedItem(
        string title = "Babylon.Berlin.S04E01.1080p.WEB-DL",
        long size = 800_000_000,
        int downloadTimeSeconds = 120)
    {
        return new HistoryItem(
            Guid.NewGuid(),
            new DownloadCompletion(
                title, MediaType.Show, size, DownloadStatus.Completed,
                $"tv/{title}/{title}.mkv", null, downloadTimeSeconds,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
    }

    public static HistoryItem FailedItem(
        string title = "Tagesschau.2024-01-15",
        string failMessage = "Download timed out")
    {
        return new HistoryItem(
            Guid.NewGuid(),
            new DownloadCompletion(
                title, MediaType.Show, 0, DownloadStatus.Failed,
                null, failMessage, 0,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
    }

    public static MediathekItem MediathekItem(
        string channel = "ARD",
        string topic = "Tatort",
        string title = "Tatort: Der letzte Fall",
        int duration = 5400)
    {
        return new MediathekItem(
            channel, topic, title,
            "Eine spannende Episode",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            duration, 500_000_000,
            "https://example.com/low.mp4",
            "https://example.com/video.mp4",
            "https://example.com/hd.mp4",
            "https://example.com/sub.xml",
            "https://example.com/website");
    }

    public static QueryMediathekCompleted MediathekSearchResult(int count = 3)
    {
        var items = Enumerable.Range(0, count)
            .Select(i => MediathekItem(title: $"Tatort Episode {i + 1}"))
            .ToArray();
        return new QueryMediathekCompleted(items, count);
    }
}
