using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class DownloadsApiEndpoints
{
    private static readonly TimeSpan _askTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _sseInterval = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public static WebApplication MapDownloadsApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/downloads")
            .WithTags("Downloads")
            .AddEndpointFilter<EndpointExceptionFilter>();

        group.MapGet("/queue", async (IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var response = await manager.Ask<QueueResponse>(new QueryQueue(), _askTimeout);
            return response switch
            {
                QueueResult result => Results.Ok(result.ToApi()),
                QueueFailed failed => Results.Problem(failed.Cause.Message, statusCode: 502),
                _ => Results.Problem("Unexpected response", statusCode: 500),
            };
        })
        .WithSummary("Get download queue")
        .Produces<ApiModels.DownloadQueueResponse>()
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
                    var queueResponse = await manager.Ask<QueueResponse>(new QueryQueue(), _askTimeout, ct);
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
        .ExcludeFromDescription();

        group.MapGet("/history", async (int? start, int? limit, string? category, IActorRegistry registry) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            var result = await history.Ask<HistoryResult>(
                new QueryHistory(start ?? 0, limit ?? 25, ParseMediaType(category)), _askTimeout);
            return Results.Ok(result.ToApi());
        })
        .WithSummary("Get download history")
        .Produces<ApiModels.DownloadHistoryResponse>()
        .ProducesProblem(504);

        group.MapGet("/history/stats", async (IActorRegistry registry) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            var result = await history.Ask<HistoryStatsResult>(new QueryHistoryStats(), _askTimeout);
            return Results.Ok(new ApiModels.HistoryStatsResponse(
                result.TotalCompleted, result.TotalFailed, result.TotalBytes,
                result.AverageDownloadTimeSeconds, result.SuccessRate));
        })
        .WithSummary("Get download statistics")
        .Produces<ApiModels.HistoryStatsResponse>()
        .ProducesProblem(504);

        group.MapGet("/history/categories", async (IActorRegistry registry) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            var result = await history.Ask<HistoryCategoriesResult>(new QueryHistoryCategories(), _askTimeout);
            return Results.Ok(result.Categories);
        })
        .WithSummary("List download categories")
        .Produces<string[]>()
        .ProducesProblem(504);

        group.MapDelete("/queue/{id:guid}", async (Guid id, IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<DeleteDownloadResult>(new DeleteDownload(id), _askTimeout);
            return result.Success
                ? Results.Ok(new ApiModels.OperationResult(true))
                : Results.NotFound(new ApiModels.OperationResult(false, result.Error));
        })
        .WithSummary("Cancel download")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapDelete("/history/{id:guid}", async (Guid id, IActorRegistry registry) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            var result = await history.Ask<DeleteDownloadResult>(new RemoveHistoryEntry(id), _askTimeout);
            return result.Success
                ? Results.Ok(new ApiModels.OperationResult(true))
                : Results.NotFound(new ApiModels.OperationResult(false, result.Error));
        })
        .WithSummary("Delete history entry")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/{id:guid}/retry", async (Guid id, IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            history.Tell(new RemoveHistoryEntry(id));
            var result = await manager.Ask<RetryDownloadResult>(new RetryDownload(id), _askTimeout);
            return result.Success
                ? Results.Ok(new ApiModels.OperationResult(true))
                : Results.BadRequest(new ApiModels.OperationResult(false, result.Error));
        })
        .WithSummary("Retry failed download")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/pause", async (IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<PauseDownloadsResult>(new PauseDownloads(), _askTimeout);
            return Results.Ok(new ApiModels.OperationResult(result.Success));
        })
        .WithSummary("Pause download pipeline")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/resume", async (IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<ResumeDownloadsResult>(new ResumeDownloads(), _askTimeout);
            return Results.Ok(new ApiModels.OperationResult(result.Success));
        })
        .WithSummary("Resume download pipeline")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/queue/{id:guid}/force-start", async (Guid id, IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<ForceStartDownloadResult>(new ForceStartDownload(id), _askTimeout);
            return result.Success
                ? Results.Ok(new ApiModels.OperationResult(true))
                : Results.BadRequest(new ApiModels.OperationResult(false, result.Error));
        })
        .WithSummary("Force-start a queued download")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/queue/{id:guid}/move", async (Guid id, ApiModels.MoveRequest body, IActorRegistry registry) =>
        {
            DownloadPriority? priority = null;
            if (body.Priority is not null && !Enum.TryParse(body.Priority, true, out DownloadPriority parsed))
            {
                return Results.BadRequest(new ApiModels.OperationResult(false, "Invalid priority. Use High, Normal, or Low."));
            }
            else if (body.Priority is not null)
            {
                priority = Enum.Parse<DownloadPriority>(body.Priority, true);
            }

            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<MoveDownloadResponse>(new MoveDownload(id, body.Position, priority), _askTimeout);
            return result switch
            {
                MoveDownloadCompleted => Results.Ok(new ApiModels.OperationResult(true)),
                MoveDownloadFailed f => Results.NotFound(new ApiModels.OperationResult(false, f.Reason)),
                _ => Results.Problem("Unexpected response", statusCode: 500),
            };
        })
        .WithSummary("Move download to position")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/queue/{id:guid}/priority", async (Guid id, ApiModels.PriorityRequest body, IActorRegistry registry) =>
        {
            if (!Enum.TryParse<DownloadPriority>(body.Priority, true, out var priority))
            {
                return Results.BadRequest(new ApiModels.OperationResult(false, "Invalid priority. Use High, Normal, or Low."));
            }

            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<SetDownloadPriorityResponse>(new SetDownloadPriority(id, priority), _askTimeout);
            return result switch
            {
                SetDownloadPriorityCompleted => Results.Ok(new ApiModels.OperationResult(true)),
                SetDownloadPriorityFailed f => Results.NotFound(new ApiModels.OperationResult(false, f.Reason)),
                _ => Results.Problem("Unexpected response", statusCode: 500),
            };
        })
        .WithSummary("Set download priority")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/queue/swap", async (ApiModels.SwapRequest body, IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<SwapDownloadsResponse>(new SwapDownloads(body.Id1, body.Id2), _askTimeout);
            return result switch
            {
                SwapDownloadsCompleted => Results.Ok(new ApiModels.OperationResult(true)),
                SwapDownloadsFailed f => Results.BadRequest(new ApiModels.OperationResult(false, f.Reason)),
                _ => Results.Problem("Unexpected response", statusCode: 500),
            };
        })
        .WithSummary("Swap two downloads")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapGet("/settings", (IOptionsMonitor<DownloadOptions> options) =>
        {
            var opts = options.CurrentValue;
            var schedule = opts.DownloadSchedule
                .Select(s => new ApiModels.DownloadTimeSlotResponse(
                    s.Start.ToString("HH:mm"), s.End.ToString("HH:mm")))
                .ToArray();
            return Results.Ok(new ApiModels.DownloadSettingsResponse(
                opts.ConcurrentDownloads, schedule));
        })
        .WithSummary("Get download settings")
        .Produces<ApiModels.DownloadSettingsResponse>();

        return app;
    }

    private static MediaType? ParseMediaType(string? value) => value switch
    {
        "movie" or "movies" => MediaType.Movie,
        "tv" or "show" => MediaType.Show,
        _ => null,
    };
}
