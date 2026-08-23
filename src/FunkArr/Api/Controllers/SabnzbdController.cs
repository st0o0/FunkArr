using System.Globalization;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Models;
using FunkArr.Configuration;
using FunkArr.DownloadClient;
using FunkArr.Indexer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FunkArr.Api.Controllers;

[ApiController]
[Route("download/api")]
[Route("sabnzbd/api")]
[Tags("SABnzbd Emulation")]
public sealed class SabnzbdController(
    ActorRegistry actorRegistry,
    IOptions<DownloadOptions> downloadOptions) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    [HttpGet("")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> HandleGet([FromQuery] string? mode)
    {
        var normalizedMode = mode?.ToLowerInvariant();

        return normalizedMode switch
        {
            "version" => Ok(new SabnzbdVersionResponse("4.3.3")),
            "get_config" => HandleGetConfig(),
            "queue" => await HandleQueue(),
            "history" => await HandleHistory(),
            _ => Ok(new SabnzbdErrorResponse(false, $"Unknown mode: {mode}")),
        };
    }

    [HttpPost("")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(200)]
    public async Task<IActionResult> HandlePost([FromQuery] string? mode, [FromForm] IFormFile? file)
    {
        var normalizedMode = mode?.ToLowerInvariant();

        if (normalizedMode == "addfile")
        {
            return await HandleAddFile(file);
        }

        return Ok(new SabnzbdErrorResponse(false, $"Unknown mode: {mode}"));
    }

    private IActionResult HandleGetConfig()
    {
        return Ok(new SabnzbdConfigResponse(new SabnzbdConfig(
            new SabnzbdMiscConfig(downloadOptions.Value.DownloadPath ?? string.Empty),
            [
                new SabnzbdCategory("tv", "tv", 0, ""),
                new SabnzbdCategory("movies", "movies", 1, ""),
            ])));
    }

    private async Task<IActionResult> HandleQueue()
    {
        var queueCoordinator = await actorRegistry.GetAsync<QueueCoordinator>();
        var trackerShard = actorRegistry.Get<DownloadRequestTracker>();

        var orderResponse = await queueCoordinator.Ask<QueueCoordinator.QueueOrderResponse>(
            new QueueCoordinator.GetQueueOrder(), AskTimeout);

        var statusTasks = orderResponse.Entries
            .Select(e => trackerShard.Ask<DownloadRequestTracker.StatusResponse>(
                new DownloadRequestTracker.GetStatus(e.NzoId), AskTimeout))
            .ToList();

        var statuses = await Task.WhenAll(statusTasks);

        var slots = statuses.Select(s => new SabnzbdQueueSlot(
            s.NzoId,
            s.Title,
            s.Status switch
            {
                "Downloading" => "Downloading",
                "Muxing" => "Extracting",
                _ => "Queued",
            },
            "0",
            "0.00",
            "0.00",
            "0:00:00")).ToArray();

        return Ok(new SabnzbdQueueResponse(new SabnzbdQueue(
            slots.Length > 0 ? "Downloading" : "Idle",
            slots,
            "0 B/s",
            "0:00:00",
            "0.00",
            "0.00")));
    }

    private async Task<IActionResult> HandleHistory()
    {
        var queueCoordinator = await actorRegistry.GetAsync<QueueCoordinator>();
        var trackerShard = actorRegistry.Get<DownloadRequestTracker>();

        var completedResponse = await queueCoordinator.Ask<QueueCoordinator.CompletedJobIdsResponse>(
            new QueueCoordinator.GetCompletedJobIds(), AskTimeout);

        var pathMapping = PathMappingHelper.ParsePathMapping(downloadOptions.Value.PathMapping);

        var historyTasks = completedResponse.NzoIds
            .Select(nzoId => trackerShard.Ask<DownloadRequestTracker.HistoryEntryResponse>(
                new DownloadRequestTracker.GetHistoryEntry(nzoId), AskTimeout))
            .ToList();

        var entries = await Task.WhenAll(historyTasks);

        var slots = entries.Select(e => new SabnzbdHistorySlot(
            e.NzoId,
            e.Title,
            e.Status,
            PathMappingHelper.MapPath(e.OutputPath ?? string.Empty, pathMapping),
            e.CompletedAt?.ToUnixTimeSeconds() ?? 0,
            e.ErrorMessage ?? string.Empty)).ToArray();

        return Ok(new SabnzbdHistoryResponse(new SabnzbdHistory(slots, slots.Length)));
    }

    private async Task<IActionResult> HandleAddFile(IFormFile? file)
    {
        if (file is null)
        {
            return Ok(new SabnzbdAddFileResponse(false, Error: "No file uploaded"));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var nzbContent = await reader.ReadToEndAsync();
        var (url, title, subtitleUrl) = FakeNzbBuilder.ParseFakeNzb(nzbContent);

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(title))
        {
            return Ok(new SabnzbdAddFileResponse(false, Error: "Could not extract download URL from NZB"));
        }

        var queueCoordinator = await actorRegistry.GetAsync<QueueCoordinator>();
        var nzoId = await queueCoordinator.Ask<string>(
            new QueueCoordinator.Enqueue(url, title, subtitleUrl), AskTimeout);

        return Ok(new SabnzbdAddFileResponse(true, NzoIds: [nzoId]));
    }
}
