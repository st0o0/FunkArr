using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Newznab.Services;
using FunkArr.ArrApi.Sabnzbd.Models;
using FunkArr.Core;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FunkArr.ArrApi.Sabnzbd.Services;

public sealed class SabnzbdDownloadService(
    IActorRegistry registry,
    ILogger<SabnzbdDownloadService> logger)
{
    internal async Task<SabnzbdResult<SabnzbdAddResponse>> AddFile(IFormFile? file, string? cat, string? priority, CancellationToken ct)
    {
        if (file is null)
        {
            return new SabnzbdError("No NZB file uploaded");
        }

        NzbParseResult parsed;
        try
        {
            await using var stream = file.OpenReadStream();
            var result = NzbService.ParseNzb(stream);
            if (result is null)
            {
                return new SabnzbdError("Invalid NZB file: not valid XML");
            }

            parsed = result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse NZB file");
            return new SabnzbdError("Invalid NZB file: not valid XML");
        }

        var videoUrl = parsed.Meta(FunkArrHeaders.Url) ?? parsed.Meta("url");
        if (string.IsNullOrEmpty(videoUrl))
        {
            return new SabnzbdError("Invalid NZB format: missing video URL");
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
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var media = new DownloadMedia(title, videoUrl, subtitleUrl, channel, duration, size, category);
            var addCmd = new AddDownload(media, downloadPriority);
            var addResult = await manager.Ask<DownloadAdded>(addCmd, ct);

            if (priority == "2")
            {
                manager.Tell(new ForceStartDownload(addResult.DownloadId));
            }

            return new SabnzbdAddResponse(true, [addResult.DownloadId.ToString()]);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout adding download for {Title}", title);
            return new SabnzbdError("Request timed out", 504);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add download for {Title}", title);
            return new SabnzbdError("Download request failed");
        }
    }

    internal async Task<SabnzbdResult<SabnzbdStatusResponse>> DeleteFromQueue(string? nzoId, CancellationToken ct)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return new SabnzbdError("Item not found");
        }

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<DeleteDownloadResult>(new DeleteDownload(downloadId), ct);
            return result.Success
                ? new SabnzbdStatusResponse(true)
                : new SabnzbdError(result.Error ?? "Delete failed");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout deleting download {DownloadId}", downloadId);
            return new SabnzbdError("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult<SabnzbdStatusResponse>> DeleteFromHistory(string? nzoId, CancellationToken ct)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return new SabnzbdError("Item not found");
        }

        try
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>(ct);
            var result = await history.Ask<DeleteDownloadResult>(new RemoveHistoryEntry(downloadId), ct);
            return result.Success
                ? new SabnzbdStatusResponse(true)
                : new SabnzbdError(result.Error ?? "Delete failed");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout deleting history entry {DownloadId}", downloadId);
            return new SabnzbdError("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult<SabnzbdStatusResponse>> Retry(string? nzoId, CancellationToken ct)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return new SabnzbdError("Item not found");
        }

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var history = await registry.GetAsync<IDownloadHistoryManager>(ct);

            history.Tell(new RemoveHistoryEntry(downloadId));
            var result = await manager.Ask<RetryDownloadResult>(new RetryDownload(downloadId), ct);
            return result.Success
                ? new SabnzbdStatusResponse(true)
                : new SabnzbdError(result.Error ?? "Retry failed");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout retrying download {DownloadId}", downloadId);
            return new SabnzbdError("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult<SabnzbdStatusResponse>> SetPriority(string? nzoId, string? priorityValue, CancellationToken ct)
    {
        if (!Guid.TryParse(nzoId, out var downloadId))
        {
            return new SabnzbdError("Invalid nzo_id");
        }

        if (!int.TryParse(priorityValue, out var priorityInt))
        {
            return new SabnzbdError("Invalid priority value");
        }

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);

            if (priorityInt == 2)
            {
                var forceResult = await manager.Ask<ForceStartDownloadResult>(new ForceStartDownload(downloadId), ct);
                return forceResult.Success
                    ? new SabnzbdStatusResponse(true)
                    : new SabnzbdError(forceResult.Error ?? "Force start failed");
            }

            var priority = Math.Clamp(priorityInt, -1, 1) switch
            {
                -1 => DownloadPriority.Low,
                0 => DownloadPriority.Normal,
                _ => DownloadPriority.High
            };
            var result = await manager.Ask<SetDownloadPriorityResponse>(new SetDownloadPriority(downloadId, priority), ct);
            return result is SetDownloadPriorityCompleted
                ? new SabnzbdStatusResponse(true)
                : new SabnzbdError((result as SetDownloadPriorityFailed)?.Reason ?? "Priority change failed");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout setting priority for {DownloadId}", downloadId);
            return new SabnzbdError("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult<SabnzbdStatusResponse>> Swap(string? nzoId1, string? nzoId2, CancellationToken ct)
    {
        if (!Guid.TryParse(nzoId1, out var id1) || !Guid.TryParse(nzoId2, out var id2))
        {
            return new SabnzbdError("Invalid nzo_id");
        }

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<SwapDownloadsResponse>(new SwapDownloads(id1, id2), ct);
            return result is SwapDownloadsCompleted
                ? new SabnzbdStatusResponse(true)
                : new SabnzbdError((result as SwapDownloadsFailed)?.Reason ?? "Swap failed");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout swapping downloads {Id1} and {Id2}", id1, id2);
            return new SabnzbdError("Request timed out", 504);
        }
    }
}
