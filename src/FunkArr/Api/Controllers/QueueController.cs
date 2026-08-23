using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Models;
using FunkArr.Configuration;
using FunkArr.DownloadClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FunkArr.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Tags("Download Queue")]
public sealed class QueueController(ActorRegistry actorRegistry, IOptions<DownloadOptions> downloadOptions) : ControllerBase
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    [HttpGet("queue")]
    [ProducesResponseType<QueueItemResponse[]>(200)]
    public async Task<ActionResult<QueueItemResponse[]>> GetQueue()
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

        var jobs = statuses
            .Select(s => new QueueItemResponse(
                s.NzoId,
                s.Title,
                s.Status,
                0,
                0,
                0,
                s.EnqueuedAt))
            .ToArray();

        return Ok(jobs);
    }

    [HttpGet("history")]
    [ProducesResponseType<HistoryItemResponse[]>(200)]
    public async Task<ActionResult<HistoryItemResponse[]>> GetHistory()
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

        var jobs = entries.Select(e => new HistoryItemResponse(
            e.NzoId,
            e.Title,
            e.Status,
            PathMappingHelper.MapPath(e.OutputPath ?? string.Empty, pathMapping),
            e.ErrorMessage,
            e.CompletedAt ?? DateTimeOffset.MinValue,
            e.CompletedAt)).ToArray();

        return Ok(jobs);
    }
}
