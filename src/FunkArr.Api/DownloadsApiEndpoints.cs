using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Api.Infrastructure;
using FunkArr.Api.Models.Downloads;
using FunkArr.Api.Models.Shared;
using FunkArr.Api.Validation;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FunkArr.Api;

public static class DownloadsApiEndpoints
{
    private static readonly TimeSpan _sseInterval = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public static WebApplication MapDownloadsApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/downloads")
            .WithTags("Downloads")
            .AddEndpointFilter<ValidationEndpointFilter>()
            .AddEndpointFilter<EndpointExceptionFilter>();
        group.WithRequestTimeout(TimeSpan.FromSeconds(15));

        group.MapGet("/queue", async (IActorRegistry registry, CancellationToken ct) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var response = await manager.Ask<QueueResponse>(new QueryQueue(), ct);
            return response switch
            {
                QueueResult result => Results.Ok(result.ToApi()),
                QueueFailed failed => Results.Problem(failed.Cause.Message, statusCode: 502),
                _ => ApiResults.UnexpectedResponse(response)
            };
        })
        .WithSummary("Get download queue")
        .WithDescription("Returns all queued and active downloads with progress, speed, and priority information.")
        .Produces<DownloadQueueResponse>()
        .ProducesProblem(502)
        .ProducesProblem(504);

        group.MapGet("/queue/stream", async (HttpContext ctx, IActorRegistry registry) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            var manager = await registry.GetAsync<IDownloadManager>();
            var ct = ctx.RequestAborted;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var queueResponse = await manager.Ask<QueueResponse>(new QueryQueue(), ct);
                    if (queueResponse is not QueueResult result)
                    {
                        continue;
                    }

                    var response = result.ToApi();
                    var json = JsonSerializer.Serialize(response, _jsonOptions);

                    await ctx.Response.WriteAsync($"event: queue\ndata: {json}\n\n", ct);
                    await ctx.Response.Body.FlushAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Skip this tick on actor timeout, retry next interval
                }

                try
                {
                    await Task.Delay(_sseInterval, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        })
        .WithSummary("Stream download queue (SSE)")
        .DisableRequestTimeout()
        .ExcludeFromDescription();

        group.MapGet("/history", async ([AsParameters] DownloadHistoryRequest req, IActorRegistry registry, CancellationToken ct) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>(ct);
            var result = await history.Ask<HistoryResult>(
                new QueryHistory(req.Start ?? 0, req.Limit ?? 25, req.Category?.ToMessage()), ct);
            return Results.Ok(result.ToApi());
        })
        .WithSummary("Get download history")
        .WithDescription("Returns completed and failed downloads with pagination. Filter by category using the category parameter.")
        .Produces<DownloadHistoryResponse>()
        .ProducesProblem(504);

        group.MapGet("/history/stats", async (IActorRegistry registry, CancellationToken ct) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>(ct);
            var result = await history.Ask<HistoryStatsResult>(new QueryHistoryStats(), ct);
            return Results.Ok(new HistoryStatsResponse(
                result.TotalCompleted, result.TotalFailed, result.TotalBytes,
                result.AverageDownloadTimeSeconds, result.SuccessRate));
        })
        .WithSummary("Get download statistics")
        .WithDescription("Returns aggregate statistics over all download history: completed/failed counts, total bytes, average download time, and success rate.")
        .Produces<HistoryStatsResponse>()
        .ProducesProblem(504);

        group.MapGet("/history/categories", async (IActorRegistry registry, CancellationToken ct) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>(ct);
            var result = await history.Ask<HistoryCategoriesResult>(new QueryHistoryCategories(), ct);
            return Results.Ok(result.Categories);
        })
        .WithSummary("List download categories")
        .WithDescription("Returns the distinct categories present in download history, used for filtering.")
        .Produces<string[]>()
        .ProducesProblem(504);

        group.MapDelete("/queue/{id:guid}", async (Guid id, IActorRegistry registry, CancellationToken ct) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<DeleteDownloadResult>(new DeleteDownload(id), ct);
            return result.Success
                ? Results.Ok(new OperationResult(true))
                : Results.NotFound(new OperationResult(false, result.Error));
        })
        .WithSummary("Cancel download")
        .WithDescription("Removes a download from the queue by its ID. Returns 404 if not found.")
        .Produces<OperationResult>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapGet("/history/{id:guid}", async (Guid id, IActorRegistry registry, CancellationToken ct) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>(ct);
            var result = await history.Ask<SingleHistoryItemResult>(new QuerySingleHistoryItem(id), ct);
            return result.Item is not null
                ? Results.Ok(result.Item.ToApi())
                : Results.NotFound();
        })
        .WithSummary("Get single history item")
        .WithDescription("Returns a single completed or failed download by its ID. Returns 404 if not found.")
        .Produces<DownloadHistoryItem>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapDelete("/history/{id:guid}", async (Guid id, IActorRegistry registry, CancellationToken ct) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>(ct);
            var result = await history.Ask<DeleteDownloadResult>(new RemoveHistoryEntry(id), ct);
            return result.Success
                ? Results.Ok(new OperationResult(true))
                : Results.NotFound(new OperationResult(false, result.Error));
        })
        .WithSummary("Delete history entry")
        .WithDescription("Removes a completed or failed download from history by its ID. Returns 404 if not found.")
        .Produces<OperationResult>()
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapPost("/queue/{id:guid}/retry", async (Guid id, IActorRegistry registry, CancellationToken ct) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<RetryDownloadResult>(new RetryDownload(id), ct);
            return result.Success
                ? Results.Ok(new OperationResult(true))
                : Results.BadRequest(new OperationResult(false, result.Error));
        })
        .WithSummary("Retry failed download")
        .WithDescription("Re-enqueues a failed download for another attempt. Returns 400 if the download is not in a failed state.")
        .Produces<OperationResult>()
        .ProducesProblem(400)
        .ProducesProblem(504);

        group.MapPost("/queue/pause", async (IActorRegistry registry, CancellationToken ct) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<PauseDownloadsResult>(new PauseDownloads(), ct);
            return Results.Ok(new OperationResult(result.Success));
        })
        .WithSummary("Pause download pipeline")
        .WithDescription("Pauses all active and queued downloads. New downloads will be queued but not started.")
        .Produces<OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/queue/resume", async (IActorRegistry registry, CancellationToken ct) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<ResumeDownloadsResult>(new ResumeDownloads(), ct);
            return Results.Ok(new OperationResult(result.Success));
        })
        .WithSummary("Resume download pipeline")
        .WithDescription("Resumes the download pipeline after a pause, starting queued downloads.")
        .Produces<OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/queue/{id:guid}/force-start", async (Guid id, IActorRegistry registry, CancellationToken ct) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<ForceStartDownloadResult>(new ForceStartDownload(id), ct);
            return result.Success
                ? Results.Ok(new OperationResult(true))
                : Results.BadRequest(new OperationResult(false, result.Error));
        })
        .WithSummary("Force-start a queued download")
        .WithDescription("Immediately starts a queued download, bypassing the concurrency limit. Returns 400 if the download is not queued.")
        .Produces<OperationResult>()
        .ProducesProblem(400)
        .ProducesProblem(504);

        group.MapPost("/queue/{id:guid}/move", async (Guid id, MoveRequest body, IActorRegistry registry, CancellationToken ct) =>
        {
            var priority = body.Priority?.ToMessage();
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<MoveDownloadResponse>(new MoveDownload(id, body.Position, priority), ct);
            return result switch
            {
                MoveDownloadCompleted => Results.Ok(new OperationResult(true)),
                MoveDownloadFailed f => Results.NotFound(new OperationResult(false, f.Reason)),
                _ => ApiResults.UnexpectedResponse(result)
            };
        })
        .WithSummary("Move download to position")
        .WithDescription("Moves a download to a new position in the queue. Optionally changes its priority bucket.")
        .Produces<OperationResult>()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapPost("/queue/{id:guid}/priority", async (Guid id, PriorityRequest body, IActorRegistry registry, CancellationToken ct) =>
        {
            var priority = body.Priority.ToMessage();
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<SetDownloadPriorityResponse>(new SetDownloadPriority(id, priority), ct);
            return result switch
            {
                SetDownloadPriorityCompleted => Results.Ok(new OperationResult(true)),
                SetDownloadPriorityFailed f => Results.NotFound(new OperationResult(false, f.Reason)),
                _ => ApiResults.UnexpectedResponse(result)
            };
        })
        .WithSummary("Set download priority")
        .WithDescription("Changes the priority of a queued download to High, Normal, or Low.")
        .Produces<OperationResult>()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(504);

        group.MapPost("/queue/swap", async (SwapRequest body, IActorRegistry registry, CancellationToken ct) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<SwapDownloadsResponse>(new SwapDownloads(body.Id1, body.Id2), ct);
            return result switch
            {
                SwapDownloadsCompleted => Results.Ok(new OperationResult(true)),
                SwapDownloadsFailed f => Results.BadRequest(new OperationResult(false, f.Reason)),
                _ => ApiResults.UnexpectedResponse(result)
            };
        })
        .WithSummary("Swap two downloads")
        .WithDescription("Swaps the queue positions of two downloads.")
        .Produces<OperationResult>()
        .ProducesProblem(400)
        .ProducesProblem(504);

        group.MapGet("/settings", (IOptionsMonitor<DownloadOptions> options) =>
        {
            var opts = options.CurrentValue;
            var schedule = opts.DownloadSchedule
                .Select(s => new DownloadTimeSlotResponse(s.Start, s.End))
                .ToArray();
            return Results.Ok(new DownloadSettingsResponse(
                opts.ConcurrentDownloads, schedule));
        })
        .WithSummary("Get download settings")
        .WithDescription("Returns current download configuration including concurrency limit and schedule time slots.")
        .Produces<DownloadSettingsResponse>();

        return app;
    }

}
