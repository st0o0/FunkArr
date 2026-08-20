using Akka.Actor;
using Akka.Hosting;
using Asp.Versioning;
using FunkArr.Api.Models;
using FunkArr.Configuration;
using FunkArr.DownloadClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FunkArr.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[Tags("Download Queue")]
public sealed class QueueController(ActorRegistry actorRegistry, IOptions<DownloadOptions> downloadOptions) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    [HttpGet("queue")]
    [ProducesResponseType<QueueItemResponse[]>(200)]
    public async Task<ActionResult<QueueItemResponse[]>> GetQueue()
    {
        var queueActor = await actorRegistry.GetAsync<DownloadQueueActor>();
        var response = await queueActor.Ask<DownloadQueueActor.QueueResponse>(
            new DownloadQueueActor.GetQueue(), AskTimeout);

        var jobs = response.Jobs
            .Where(j => j.Status is DownloadStatus.Queued or DownloadStatus.Downloading or DownloadStatus.Muxing)
            .Select(j => new QueueItemResponse(
                j.NzoId,
                j.Title,
                j.Status.ToString(),
                j.ProgressPercent,
                j.DownloadedBytes,
                j.TotalBytes,
                j.EnqueuedAt))
            .ToArray();

        return Ok(jobs);
    }

    [HttpGet("history")]
    [ProducesResponseType<HistoryItemResponse[]>(200)]
    public async Task<ActionResult<HistoryItemResponse[]>> GetHistory()
    {
        var queueActor = await actorRegistry.GetAsync<DownloadQueueActor>();
        var response = await queueActor.Ask<DownloadQueueActor.HistoryResponse>(
            new DownloadQueueActor.GetHistory(), AskTimeout);

        var pathMapping = PathMappingHelper.ParsePathMapping(downloadOptions.Value.PathMapping);

        var jobs = response.Jobs.Select(j => new HistoryItemResponse(
            j.NzoId,
            j.Title,
            j.Status.ToString(),
            PathMappingHelper.MapPath(j.OutputPath ?? string.Empty, pathMapping),
            j.ErrorMessage,
            j.EnqueuedAt,
            j.CompletedAt)).ToArray();

        return Ok(jobs);
    }
}
