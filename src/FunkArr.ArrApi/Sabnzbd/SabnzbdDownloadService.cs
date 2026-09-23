using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Newznab;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Microsoft.AspNetCore.Http;
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

    internal async Task<SabnzbdResult> AddFile(IFormFile? file, string? cat, string? priority)
    {
        if (file is null)
        {
            return new SabnzbdResult.Error("No NZB file uploaded");
        }

        NzbParseResult parsed;
        try
        {
            await using var stream = file.OpenReadStream();
            var result = NzbService.ParseNzb(stream);
            if (result is null)
            {
                return new SabnzbdResult.Error("Invalid NZB file: not valid XML");
            }

            parsed = result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse NZB file");
            return new SabnzbdResult.Error("Invalid NZB file: not valid XML");
        }

        var videoUrl = parsed.Meta(FunkArrHeaders.Url) ?? parsed.Meta("url");
        if (string.IsNullOrEmpty(videoUrl))
        {
            return new SabnzbdResult.Error("Invalid NZB format: missing video URL");
        }

        var title = parsed.Meta("title") ?? "Unknown";
        var subtitleUrl = parsed.Meta(FunkArrHeaders.SubtitleUrl);
        var channel = parsed.Meta(FunkArrHeaders.Channel) ?? "";
        _ = int.TryParse(parsed.Meta(FunkArrHeaders.Duration), out var duration);
        _ = long.TryParse(parsed.Meta(FunkArrHeaders.Size), out var size);
        var categoryStr = cat ?? parsed.Meta(FunkArrHeaders.Category) ?? "";
        var category = SabnzbdResponseMapper.ParseMediaType(categoryStr);
        var downloadPriority = SabnzbdResponseMapper.MapSabnzbdPriority(priority);

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var addCmd = new AddDownload(title, videoUrl, subtitleUrl, channel, duration, size, category, downloadPriority);
            var addResult = await manager.Ask<DownloadAdded>(addCmd, Timeout);

            if (priority == "2")
            {
                manager.Tell(new ForceStartDownload(addResult.DownloadId));
            }

            return new SabnzbdResult.Ok(new { status = true, nzo_ids = new[] { addResult.DownloadId.ToString() } });
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout adding download for {Title}", title);
            return new SabnzbdResult.Error("Request timed out", 504);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add download for {Title}", title);
            return new SabnzbdResult.Error("Download request failed");
        }
    }

    internal async Task<SabnzbdResult> DeleteFromQueue(string? nzoId)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return new SabnzbdResult.Error("Item not found");
        }

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<DeleteDownloadResult>(new DeleteDownload(downloadId), Timeout);
            return result.Success
                ? new SabnzbdResult.Ok(new { status = true })
                : new SabnzbdResult.Error(result.Error ?? "Delete failed");
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout deleting download {DownloadId}", downloadId);
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult> DeleteFromHistory(string? nzoId)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return new SabnzbdResult.Error("Item not found");
        }

        try
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            var result = await history.Ask<DeleteDownloadResult>(new RemoveHistoryEntry(downloadId), Timeout);
            return result.Success
                ? new SabnzbdResult.Ok(new { status = true })
                : new SabnzbdResult.Error(result.Error ?? "Delete failed");
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout deleting history entry {DownloadId}", downloadId);
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult> Retry(string? nzoId)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return new SabnzbdResult.Error("Item not found");
        }

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var history = await registry.GetAsync<IDownloadHistoryManager>();

            history.Tell(new RemoveHistoryEntry(downloadId));
            var result = await manager.Ask<RetryDownloadResult>(new RetryDownload(downloadId), Timeout);
            return result.Success
                ? new SabnzbdResult.Ok(new { status = true })
                : new SabnzbdResult.Error(result.Error ?? "Retry failed");
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout retrying download {DownloadId}", downloadId);
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult> SetPriority(string? nzoId, string? priorityValue)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return new SabnzbdResult.Error("Invalid nzo_id");
        }

        if (!int.TryParse(priorityValue, out var priorityInt))
        {
            return new SabnzbdResult.Error("Invalid priority value");
        }

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>();

            if (priorityInt == 2)
            {
                var forceResult = await manager.Ask<ForceStartDownloadResult>(new ForceStartDownload(downloadId), Timeout);
                return forceResult.Success
                    ? new SabnzbdResult.Ok(new { status = true })
                    : new SabnzbdResult.Error(forceResult.Error ?? "Force start failed");
            }

            var priority = (DownloadPriority)Math.Clamp(priorityInt, -1, 1);
            var result = await manager.Ask<SetDownloadPriorityResponse>(new SetDownloadPriority(downloadId, priority), Timeout);
            return result is SetDownloadPriorityCompleted
                ? new SabnzbdResult.Ok(new { status = true })
                : new SabnzbdResult.Error((result as SetDownloadPriorityFailed)?.Reason ?? "Priority change failed");
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout setting priority for {DownloadId}", downloadId);
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult> Swap(string? nzoId1, string? nzoId2)
    {
        if (!Guid.TryParse(nzoId1, out var id1) || !Guid.TryParse(nzoId2, out var id2))
        {
            return new SabnzbdResult.Error("Invalid nzo_id");
        }

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<SwapDownloadsResponse>(new SwapDownloads(id1, id2), Timeout);
            return result is SwapDownloadsCompleted
                ? new SabnzbdResult.Ok(new { status = true })
                : new SabnzbdResult.Error((result as SwapDownloadsFailed)?.Reason ?? "Swap failed");
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout swapping downloads {Id1} and {Id2}", id1, id2);
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }
}
