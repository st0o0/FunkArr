using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Contracts;
using FunkArr.Configuration;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Queue;
using FunkArr.DownloadClient.Tracker;
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
    [ProducesResponseType<QueueItem[]>(200)]
    public async Task<ActionResult<QueueItem[]>> GetQueue()
    {
        var QueueActor = await actorRegistry.GetAsync<QueueActor>();
        var trackerShard = actorRegistry.Get<DownloadRequestActor>();

        var orderResponse = await QueueActor.Ask<QueueActor.QueueOrderResponse>(
            new QueueActor.GetQueueOrder(), AskTimeout);

        var statusTasks = orderResponse.Entries
            .Select(e => trackerShard.Ask<DownloadRequestActor.DownloadStatus>(
                new DownloadRequestActor.QueryStatus(e.NzoId), AskTimeout))
            .ToList();

        var statuses = await Task.WhenAll(statusTasks);

        var jobs = statuses
            .Select(s => new QueueItem(s.Category, 0, s.EnqueuedAt, s.NzoId, 0, s.Status, s.Title, 0))
            .ToArray();

        return Ok(jobs);
    }

    [HttpGet("history")]
    [ProducesResponseType<HistoryItem[]>(200)]
    public async Task<ActionResult<HistoryItem[]>> GetHistory()
    {
        var QueueActor = await actorRegistry.GetAsync<QueueActor>();
        var trackerShard = actorRegistry.Get<DownloadRequestActor>();

        var completedResponse = await QueueActor.Ask<QueueActor.CompletedJobIdsResponse>(
            new QueueActor.GetCompletedJobIds(), AskTimeout);

        var pathMapping = PathMappingHelper.ParsePathMapping(downloadOptions.Value.PathMapping);

        var historyTasks = completedResponse.NzoIds
            .Select(nzoId => trackerShard.Ask<DownloadRequestActor.DownloadHistoryEntry>(
                new DownloadRequestActor.QueryHistory(nzoId), AskTimeout))
            .ToList();

        var entries = await Task.WhenAll(historyTasks);

        var jobs = entries.Select(e => new HistoryItem(
            e.Category,
            e.CompletedAt ?? DateTimeOffset.MinValue,
            e.CompletedAt ?? DateTimeOffset.MinValue,
            e.ErrorMessage,
            e.NzoId,
            PathMappingHelper.MapPath(e.OutputPath ?? string.Empty, pathMapping),
            e.Status,
            e.Title)).ToArray();

        return Ok(jobs);
    }
}
