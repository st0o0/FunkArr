using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Newznab;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi.Sabnzbd;

public sealed class SabnzbdDownloadService(
    IActorRegistry registry,
    NzbService nzbService,
    IOptions<ArrApiOptions> options,
    ILogger<SabnzbdDownloadService> logger)
{
    private TimeSpan Timeout => TimeSpan.FromSeconds(options.Value.DownloadTimeoutSeconds);

    internal async Task<IActionResult> AddFile(IFormFile? file, string? cat, string? priority)
    {
        if (file is null)
            return new JsonResult(new { status = false, error = "No NZB file uploaded" }) { StatusCode = 400 };

        NzbParseResult parsed;
        try
        {
            await using var stream = file.OpenReadStream();
            var result = nzbService.ParseNzb(stream);
            if (result is null)
                return new JsonResult(new { status = false, error = "Invalid NZB file: not valid XML" }) { StatusCode = 400 };
            parsed = result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse NZB file");
            return new JsonResult(new { status = false, error = "Invalid NZB file: not valid XML" }) { StatusCode = 400 };
        }

        var videoUrl = parsed.Meta(FunkArrHeaders.Url) ?? parsed.Meta("url");
        if (string.IsNullOrEmpty(videoUrl))
            return new JsonResult(new { status = false, error = "Invalid NZB format: missing video URL" }) { StatusCode = 400 };

        var title = parsed.Meta("title") ?? "Unknown";
        var subtitleUrl = parsed.Meta(FunkArrHeaders.SubtitleUrl);
        var channel = parsed.Meta(FunkArrHeaders.Channel) ?? "";
        _ = int.TryParse(parsed.Meta(FunkArrHeaders.Duration), out var duration);
        _ = long.TryParse(parsed.Meta(FunkArrHeaders.Size), out var size);
        var categoryStr = cat ?? parsed.Meta(FunkArrHeaders.Category) ?? "";
        var category = SabnzbdResponseMapper.ParseMediaType(categoryStr);
        var downloadPriority = SabnzbdResponseMapper.MapSabnzbdPriority(priority);

        var manager = await registry.GetAsync<IDownloadManager>();
        var addCmd = new AddDownload(title, videoUrl, subtitleUrl, channel, duration, size, category, downloadPriority);
        var addResult = await manager.Ask<DownloadAdded>(addCmd, Timeout);

        if (priority == "2")
            manager.Tell(new ForceStartDownload(addResult.DownloadId));

        return new JsonResult(new { status = true, nzo_ids = new[] { addResult.DownloadId.ToString() } });
    }

    internal async Task<IActionResult> DeleteFromQueue(string? nzoId)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
            return new JsonResult(new { status = false, error = "Item not found" });

        var manager = await registry.GetAsync<IDownloadManager>();
        var result = await manager.Ask<DeleteDownloadResult>(new DeleteDownload(downloadId), Timeout);
        return result.Success
            ? new JsonResult(new { status = true })
            : new JsonResult(new { status = false, error = result.Error });
    }

    internal async Task<IActionResult> DeleteFromHistory(string? nzoId)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
            return new JsonResult(new { status = false, error = "Item not found" });

        var history = await registry.GetAsync<IDownloadHistoryManager>();
        var result = await history.Ask<DeleteDownloadResult>(new RemoveHistoryEntry(downloadId), Timeout);
        return result.Success
            ? new JsonResult(new { status = true })
            : new JsonResult(new { status = false, error = result.Error });
    }

    internal async Task<IActionResult> Retry(string? nzoId)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
            return new JsonResult(new { status = false, error = "Item not found" });

        var manager = await registry.GetAsync<IDownloadManager>();
        var history = await registry.GetAsync<IDownloadHistoryManager>();

        history.Tell(new RemoveHistoryEntry(downloadId));
        var result = await manager.Ask<RetryDownloadResult>(new RetryDownload(downloadId), Timeout);
        return result.Success
            ? new JsonResult(new { status = true })
            : new JsonResult(new { status = false, error = result.Error });
    }

    internal async Task<IActionResult> SetPriority(string? nzoId, string? priorityValue)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
            return new JsonResult(new { status = false, error = "Invalid nzo_id" });

        if (!int.TryParse(priorityValue, out var priorityInt))
            return new JsonResult(new { status = false, error = "Invalid priority value" });

        var manager = await registry.GetAsync<IDownloadManager>();

        if (priorityInt == 2)
        {
            var forceResult = await manager.Ask<ForceStartDownloadResult>(new ForceStartDownload(downloadId), Timeout);
            return forceResult.Success
                ? new JsonResult(new { status = true })
                : new JsonResult(new { status = false, error = forceResult.Error });
        }

        var priority = (DownloadPriority)Math.Clamp(priorityInt, -1, 1);
        var result = await manager.Ask<SetDownloadPriorityResponse>(new SetDownloadPriority(downloadId, priority), Timeout);
        return result is SetDownloadPriorityCompleted
            ? new JsonResult(new { status = true })
            : new JsonResult(new { status = false, error = (result as SetDownloadPriorityFailed)?.Reason });
    }

    internal async Task<IActionResult> Swap(string? nzoId1, string? nzoId2)
    {
        if (!Guid.TryParse(nzoId1, out var id1) || !Guid.TryParse(nzoId2, out var id2))
            return new JsonResult(new { status = false, error = "Invalid nzo_id" });

        var manager = await registry.GetAsync<IDownloadManager>();
        var result = await manager.Ask<SwapDownloadsResponse>(new SwapDownloads(id1, id2), Timeout);
        return result is SwapDownloadsCompleted
            ? new JsonResult(new { status = true })
            : new JsonResult(new { status = false, error = (result as SwapDownloadsFailed)?.Reason });
    }
}
