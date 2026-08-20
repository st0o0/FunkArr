using System.Globalization;
using Akka.Actor;
using Akka.Hosting;
using Asp.Versioning;
using FunkArr.Configuration;
using FunkArr.DownloadClient;
using FunkArr.Indexer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FunkArr.Api.Controllers;

[ApiController]
[ApiVersionNeutral]
[Route("download/api")]
[Tags("SABnzbd Emulation")]
public sealed class SabnzbdController(
    ActorRegistry actorRegistry,
    IOptions<DownloadOptions> downloadOptions) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    [HttpGet("")]
    public async Task<IActionResult> HandleGet()
    {
        var mode = Request.Query["mode"].FirstOrDefault()?.ToLowerInvariant();
        var queueActor = await actorRegistry.GetAsync<DownloadQueueActor>();

        return mode switch
        {
            "version" => Content("4.3.3", "text/plain"),
            "get_config" => HandleGetConfig(),
            "queue" => await HandleQueue(queueActor),
            "history" => await HandleHistory(queueActor),
            _ => SabnzbdError($"Unknown mode: {mode}"),
        };
    }

    [HttpPost("")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> HandlePost()
    {
        var mode = Request.Query["mode"].FirstOrDefault()?.ToLowerInvariant();

        if (mode == "addfile")
        {
            return await HandleAddFile();
        }

        return SabnzbdError($"Unknown mode: {mode}");
    }

    private IActionResult HandleGetConfig()
    {
        var config = new
        {
            misc = new { complete_dir = downloadOptions.Value.DownloadPath },
        };
        return Ok(config);
    }

    private async Task<IActionResult> HandleQueue(IActorRef queueActor)
    {
        var response = await queueActor.Ask<DownloadQueueActor.QueueResponse>(
            new DownloadQueueActor.GetQueue(), AskTimeout);

        var slots = response.Jobs.Select(j => new
        {
            nzo_id = j.NzoId,
            filename = j.Title,
            status = j.Status switch
            {
                DownloadStatus.Downloading => "Downloading",
                DownloadStatus.Muxing => "Extracting",
                _ => "Queued",
            },
            percentage = j.ProgressPercent.ToString("F0", CultureInfo.InvariantCulture),
            mb = (j.TotalBytes / 1024.0 / 1024.0).ToString("F2", CultureInfo.InvariantCulture),
            mbleft = ((j.TotalBytes - j.DownloadedBytes) / 1024.0 / 1024.0).ToString("F2", CultureInfo.InvariantCulture),
            timeleft = "0:00:00",
        }).ToArray();

        var totalMb = response.Jobs.Sum(j => j.TotalBytes) / 1024.0 / 1024.0;
        var totalMbLeft = response.Jobs.Sum(j => j.TotalBytes - j.DownloadedBytes) / 1024.0 / 1024.0;

        return Ok(new
        {
            queue = new
            {
                status = slots.Length > 0 ? "Downloading" : "Idle",
                slots,
                speed = "0 B/s",
                timeleft = "0:00:00",
                mb = totalMb.ToString("F2", CultureInfo.InvariantCulture),
                mbleft = totalMbLeft.ToString("F2", CultureInfo.InvariantCulture),
            },
        });
    }

    private async Task<IActionResult> HandleHistory(IActorRef queueActor)
    {
        var response = await queueActor.Ask<DownloadQueueActor.HistoryResponse>(
            new DownloadQueueActor.GetHistory(), AskTimeout);

        var pathMapping = PathMappingHelper.ParsePathMapping(downloadOptions.Value.PathMapping);

        var slots = response.Jobs.Select(j => new
        {
            nzo_id = j.NzoId,
            name = j.Title,
            status = j.Status == DownloadStatus.Completed ? "Completed" : "Failed",
            storage = PathMappingHelper.MapPath(j.OutputPath ?? string.Empty, pathMapping),
            completed = j.CompletedAt?.ToUnixTimeSeconds() ?? 0,
            fail_message = j.ErrorMessage ?? string.Empty,
        }).ToArray();

        return Ok(new
        {
            history = new
            {
                slots,
                noofslots = slots.Length,
            },
        });
    }

    private async Task<IActionResult> HandleAddFile()
    {
        var form = await Request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();

        if (file is null)
        {
            return Ok(new { status = false, error = "No file uploaded" });
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var nzbContent = await reader.ReadToEndAsync();
        var (url, title, subtitleUrl) = FakeNzbBuilder.ParseFakeNzb(nzbContent);

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(title))
        {
            return Ok(new { status = false, error = "Could not extract download URL from NZB" });
        }

        var queueActor = await actorRegistry.GetAsync<DownloadQueueActor>();
        var nzoId = await queueActor.Ask<string>(
            new DownloadQueueActor.EnqueueDownload(url, title, subtitleUrl), AskTimeout);

        return Ok(new
        {
            status = true,
            nzo_ids = new[] { nzoId },
        });
    }

    private static IActionResult SabnzbdError(string message) =>
        new OkObjectResult(new { status = false, error = message });
}
