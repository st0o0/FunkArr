using System.Xml.Serialization;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Sabnzbd.Models;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi.Sabnzbd;

public static class DownloadApiEndpoints
{
    private static readonly XmlSerializer _nzbSerializer = new(typeof(Nzb));
    private static readonly TimeSpan _askTimeout = TimeSpan.FromSeconds(10);

    public static WebApplication MapDownloadApi(this WebApplication app)
    {
        var group = app.MapGroup("/download/api")
            .AddEndpointFilter(new ApiKeyEndpointFilter(
                () => Results.Json(new { status = false, error = "API Key Incorrect" }, statusCode: 403)));

        group.MapGet("/", async (
            [AsParameters] DownloadGetRequest req,
            IOptionsMonitor<DownloadOptions> downloadOptions,
            IActorRegistry registry) =>
        {
            var opts = downloadOptions.CurrentValue;
            var manager = await registry.GetAsync<IDownloadManager>();
            var history = await registry.GetAsync<IDownloadHistoryManager>();

            return (req.Mode ?? "") switch
            {
                "version" => Results.Json(new { version = "4.3.3" }),
                "get_config" => ConfigResult(opts),
                "fullstatus" => await FullStatusResult(manager, opts.CompletePath),
                "queue" when req.Name == "delete" && !string.IsNullOrEmpty(req.Value) =>
                    await DeleteFromQueueResult(manager, req.Value),
                "queue" when req.Name is not null =>
                    Results.Json(new { status = false, error = "Invalid queue command" }, statusCode: 400),
                "queue" => await QueueResult(manager, req.Start ?? 0, req.Limit ?? 0, req.Category),
                "history" when req.Name == "delete" && !string.IsNullOrEmpty(req.Value) =>
                    await DeleteFromHistoryResult(history, req.Value),
                "history" => await HistoryResult(history, req.Start ?? 0, req.Limit ?? 0, req.Category),
                "retry" when string.IsNullOrEmpty(req.Value) =>
                    Results.Json(new { status = false, error = "Missing value parameter" }, statusCode: 400),
                "retry" => await RetryResult(manager, history, req.Value),
                _ => Results.Json(new { status = false, error = "Invalid mode" }, statusCode: 400),
            };
        });

        group.MapPost("/", async (
            [AsParameters] DownloadPostRequest req,
            IFormFile? nzbfile,
            IActorRegistry registry) =>
        {
            if ((req.Mode ?? "") != "addfile")
            {
                return Results.Json(new { status = false, error = "Invalid mode" }, statusCode: 400);
            }

            if (nzbfile is null)
            {
                return Results.Json(new { status = false, error = "No NZB file uploaded" }, statusCode: 400);
            }

            await using var stream = nzbfile.OpenReadStream();
            var nzb = _nzbSerializer.Deserialize(stream) as Nzb;

            var videoUrl = Meta("X-FunkArr-Url") ?? Meta("url");
            if (videoUrl is null)
            {
                return Results.Json(new { status = false, error = "Invalid NZB format" }, statusCode: 400);
            }

            var title = Meta("title") ?? "Unknown";
            var subtitleUrl = Meta("X-FunkArr-SubtitleUrl");
            var channel = Meta("X-FunkArr-Channel") ?? "";
            _ = int.TryParse(Meta("X-FunkArr-Duration"), out var duration);
            _ = long.TryParse(Meta("X-FunkArr-Size"), out var size);
            var category = req.Cat ?? Meta("X-FunkArr-Category") ?? "";

            var manager = await registry.GetAsync<IDownloadManager>();
            var addCmd = new AddDownload(title, videoUrl, subtitleUrl, channel, duration, size, category);
            var result = await manager.Ask<DownloadAdded>(addCmd, _askTimeout);

            return Results.Json(new { status = true, nzo_ids = new[] { result.DownloadId.ToString() } });

            string? Meta(string type) => nzb?.Head.Metas.FirstOrDefault(m => m.Type == type)?.Value;
        }).DisableAntiforgery();

        return app;
    }

    private static IResult ConfigResult(DownloadOptions opts) =>
        Results.Json(new
        {
            config = new
            {
                misc = new
                {
                    complete_dir = opts.CompletePath.Replace('\\', '/'),
                    enable_tv_sorting = false,
                    enable_movie_sorting = false,
                    enable_date_sorting = false,
                    pre_check = false,
                    history_retention = "all",
                    tv_categories = Array.Empty<string>(),
                    movie_categories = Array.Empty<string>(),
                    date_categories = Array.Empty<string>(),
                },
                categories = opts.Categories.Select((c, i) => new
                {
                    name = c.Name,
                    order = i,
                    dir = string.IsNullOrEmpty(c.Dir) ? c.Name : c.Dir,
                    newzbin = "",
                    priority = 0,
                }).ToArray(),
                sorters = Array.Empty<object>(),
            },
        });

    private static async Task<IResult> FullStatusResult(IActorRef manager, string completePath)
    {
        var result = await manager.Ask<QueueResult>(new QueryQueue(), _askTimeout);
        var totalSpeed = result.Items
            .Where(i => i is { Status: DownloadStatus.Processing, Speed: > 0 })
            .Sum(i => i.TotalBytes > 0 ? i.BytesDownloaded / Math.Max(1.0, i.CurrentTimeUs / 1_000_000.0) : 0);

        return Results.Json(new FullStatusResponse(new FullStatusData(
            Paused: false,
            Speedlimit: "",
            Diskspace1: "0",
            Diskspace2: "0",
            Completedir: completePath.Replace('\\', '/'),
            Speed: ((long)totalSpeed).ToString())));
    }

    private static async Task<IResult> QueueResult(IActorRef manager, int start, int limit, string? category)
    {
        var result = await manager.Ask<QueueResult>(new QueryQueue(start, limit, category), _askTimeout);

        var slots = result.Items.Select((item, index) => new QueueSlot(
            NzoId: item.DownloadId.ToString(),
            Status: item.Status == DownloadStatus.Processing ? "Downloading" : "Queued",
            Index: start + index,
            Timeleft: FormatTimeLeft(item),
            Mb: (item.TotalBytes / 1_048_576.0).ToString("F0"),
            Filename: item.Title,
            Cat: item.Category,
            Mbleft: ((item.TotalBytes - item.BytesDownloaded) / 1_048_576.0).ToString("F0"),
            Percentage: item.TotalDuration > 0
                ? ((int)(item.CurrentTimeUs / 1_000_000.0 / item.TotalDuration * 100)).ToString()
                : "0",
            Priority: "Normal",
            Speed: FormatSpeed(item))).ToArray();

        return Results.Json(new QueueResponse(new QueueData(
            Paused: false,
            Speedlimit: "",
            NoofSlotsTotal: result.TotalItems,
            Diskspace1: "0",
            Diskspace2: "0",
            Speed: "0",
            Slots: slots)));
    }

    private static async Task<IResult> HistoryResult(IActorRef history, int start, int limit, string? category)
    {
        var result = await history.Ask<HistoryResult>(new QueryHistory(start, limit, category), _askTimeout);

        var slots = result.Items.Select(item => new HistorySlot(
            NzoId: item.DownloadId.ToString(),
            Name: item.Title,
            NzbName: item.Title + ".nzb",
            Category: item.Category,
            Bytes: item.TotalBytes,
            DownloadTime: item.DownloadTimeSeconds,
            Storage: item.FilePath,
            Status: MapHistoryStatus(item.Status),
            FailMessage: item.FailMessage,
            CompletedOn: item.CompletedAt)).ToArray();

        return Results.Json(new HistoryResponse(new HistoryData(
            NoofSlots: result.TotalItems,
            Slots: slots)));
    }

    private static string MapHistoryStatus(DownloadStatus status) => status switch
    {
        DownloadStatus.Completed => "Completed",
        _ => "Failed",
    };

    private static async Task<IResult> DeleteFromQueueResult(IActorRef manager, string nzoId)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return Results.Json(new { status = false, error = "Item not found" });
        }

        var result = await manager.Ask<DeleteDownloadResult>(new DeleteDownload(downloadId), _askTimeout);
        return result.Success
            ? Results.Json(new { status = true })
            : Results.Json(new { status = false, error = result.Error });
    }

    private static async Task<IResult> DeleteFromHistoryResult(IActorRef history, string nzoId)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return Results.Json(new { status = false, error = "Item not found" });
        }

        var result = await history.Ask<DeleteDownloadResult>(new RemoveHistoryEntry(downloadId), _askTimeout);
        return result.Success
            ? Results.Json(new { status = true })
            : Results.Json(new { status = false, error = result.Error });
    }

    private static async Task<IResult> RetryResult(IActorRef manager, IActorRef history, string nzoId)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return Results.Json(new { status = false, error = "Item not found" });
        }

        history.Tell(new RemoveHistoryEntry(downloadId));
        var result = await manager.Ask<RetryDownloadResult>(new RetryDownload(downloadId), _askTimeout);
        return result.Success
            ? Results.Json(new { status = true })
            : Results.Json(new { status = false, error = result.Error });
    }

    private static string FormatSpeed(QueueItem item)
    {
        if (item.Status != DownloadStatus.Processing || item.CurrentTimeUs <= 0)
        {
            return "0";
        }

        var elapsedSeconds = item.CurrentTimeUs / 1_000_000.0;
        var bytesPerSecond = item.BytesDownloaded / elapsedSeconds;
        return ((long)bytesPerSecond).ToString();
    }

    private static string FormatTimeLeft(QueueItem item)
    {
        if (item.TotalDuration <= 0 || item.Speed <= 0)
        {
            return "00:00:00";
        }

        var elapsedSeconds = item.CurrentTimeUs / 1_000_000.0;
        var remainingSeconds = (item.TotalDuration - elapsedSeconds) / item.Speed;
        if (remainingSeconds < 0)
        {
            remainingSeconds = 0;
        }

        var ts = TimeSpan.FromSeconds(remainingSeconds);
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
