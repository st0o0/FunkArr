using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.ArrApi.Newznab.Services;
using FunkArr.ArrApi.Sabnzbd.Models;
using FunkArr.Core.Actors;
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

        NzbPayload payload;
        try
        {
            await using var stream = file.OpenReadStream();
            var result = NzbService.ParseNzb(stream);
            if (result is null)
            {
                return new SabnzbdError("Invalid NZB file: not valid XML");
            }

            payload = result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse NZB file");
            return new SabnzbdError("Invalid NZB file: not valid XML");
        }

        if (string.IsNullOrEmpty(payload.Url))
        {
            return new SabnzbdError("Invalid NZB format: missing video URL");
        }

        var category = cat is null ? payload.Category : SabnzbdResponseMapper.ParseMediaType(cat);

        try
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var media = new DownloadMedia(
                payload.Title, payload.Url, payload.SubtitleUrl, payload.Channel, payload.Duration, payload.Size, category);
            var addCmd = new AddDownload(media, DownloadPriority.Normal);
            var addResult = await manager.Ask<DownloadAdded>(addCmd, ct);

            if (priority == "2")
            {
                manager.Tell(new ForceStartDownload(addResult.DownloadId));
            }

            return new SabnzbdAddResponse(true, [addResult.DownloadId.ToString()]);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout adding download for {Title}", payload.Title);
            return new SabnzbdError("Request timed out", 504);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add download for {Title}", payload.Title);
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

            var result = await manager.Ask<RetryDownloadResult>(new RetryDownload(downloadId), ct);
            if (result.Success)
            {
                history.Tell(new RemoveHistoryEntry(downloadId));
            }

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

}
