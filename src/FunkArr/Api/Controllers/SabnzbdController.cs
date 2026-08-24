using System.Globalization;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Contracts.Sabnzbd;
using FunkArr.Configuration;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Queue;
using FunkArr.DownloadClient.Tracker;
using FunkArr.Indexer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FunkArr.Api.Controllers;

[ApiController]
[Route("download/api")]
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
    public async Task<IActionResult> HandlePost([FromQuery] string? mode, [FromQuery] string? cat, [FromForm] IFormFile? file)
    {
        var normalizedMode = mode?.ToLowerInvariant();

        if (normalizedMode == "addfile")
        {
            return await HandleAddFile(file, cat);
        }

        return Ok(new SabnzbdErrorResponse(false, $"Unknown mode: {mode}"));
    }

    private IActionResult HandleGetConfig()
    {
        var categories = downloadOptions.Value.Category
            .Select((kvp, i) => new SabnzbdCategory(kvp.Key, kvp.Value, i, ""))
            .ToArray();

        return Ok(new SabnzbdConfigResponse(new SabnzbdConfig(
            new SabnzbdMiscConfig(downloadOptions.Value.Path ?? string.Empty),
            categories)));
    }

    private async Task<IActionResult> HandleQueue()
    {
        var queueActor = await actorRegistry.GetAsync<QueueActor>();
        var trackerShard = actorRegistry.Get<DownloadRequestActor>();

        var orderResponse = await queueActor.Ask<QueueActor.QueueOrderResponse>(
            new QueueActor.GetQueueOrder(), AskTimeout);

        var statusTasks = orderResponse.Entries
            .Select(e => trackerShard.Ask<DownloadRequestActor.DownloadStatus>(
                new DownloadRequestActor.QueryStatus(e.NzoId), AskTimeout))
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
            s.Category ?? "*",
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
        var queueActor = await actorRegistry.GetAsync<QueueActor>();
        var trackerShard = actorRegistry.Get<DownloadRequestActor>();

        var completedResponse = await queueActor.Ask<QueueActor.CompletedJobIdsResponse>(
            new QueueActor.GetCompletedJobIds(), AskTimeout);

        var pathMapping = PathMappingHelper.ParsePathMapping(downloadOptions.Value.PathMapping);

        var historyTasks = completedResponse.NzoIds
            .Select(nzoId => trackerShard.Ask<DownloadRequestActor.DownloadHistoryEntry>(
                new DownloadRequestActor.QueryHistory(nzoId), AskTimeout))
            .ToList();

        var entries = await Task.WhenAll(historyTasks);

        var slots = entries.Select(e => new SabnzbdHistorySlot(
            e.NzoId,
            e.Title,
            e.Status,
            e.Category ?? "*",
            PathMappingHelper.MapPath(e.OutputPath ?? string.Empty, pathMapping),
            e.CompletedAt?.ToUnixTimeSeconds() ?? 0,
            e.ErrorMessage ?? string.Empty)).ToArray();

        return Ok(new SabnzbdHistoryResponse(new SabnzbdHistory(slots, slots.Length)));
    }

    private async Task<IActionResult> HandleAddFile(IFormFile? file, string? cat)
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

        var queueActor = await actorRegistry.GetAsync<QueueActor>();
        var nzoId = await queueActor.Ask<string>(
            new QueueActor.Enqueue(url, title, subtitleUrl, cat), AskTimeout);

        return Ok(new SabnzbdAddFileResponse(true, NzoIds: [nzoId]));
    }
}
