using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Extensions;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class DownloadsApiEndpoints
{
    private static readonly TimeSpan _askTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _sseInterval = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public static WebApplication MapDownloadsApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/downloads").WithTags("Downloads");

        group.MapGet("/queue", async (IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            try
            {
                var result = await manager.Ask<QueueResult>(new QueryQueue(), _askTimeout);
                return Results.Ok(result.ToApi());
            }
            catch (Exception)
            {
                return ApiResults.GatewayTimeout();
            }
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
                    var result = await manager.Ask<QueueResult>(new QueryQueue(), _askTimeout, ct);
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
            try
            {
                var result = await history.Ask<HistoryResult>(
                    new QueryHistory(start ?? 0, limit ?? 25, category), _askTimeout);
                return Results.Ok(result.ToApi());
            }
            catch (Exception)
            {
                return ApiResults.GatewayTimeout();
            }
        })
        .WithSummary("Get download history")
        .Produces<ApiModels.DownloadHistoryResponse>()
        .ProducesProblem(504);

        group.MapGet("/history/stats", async (IActorRegistry registry) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            try
            {
                var result = await history.Ask<HistoryStatsResult>(new QueryHistoryStats(), _askTimeout);
                return Results.Ok(new ApiModels.HistoryStatsResponse(
                    result.TotalCompleted, result.TotalFailed, result.TotalBytes,
                    result.AverageDownloadTimeSeconds, result.SuccessRate));
            }
            catch (Exception)
            {
                return ApiResults.GatewayTimeout();
            }
        })
        .WithSummary("Get download statistics")
        .Produces<ApiModels.HistoryStatsResponse>()
        .ProducesProblem(504);

        group.MapGet("/history/categories", async (IActorRegistry registry) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            try
            {
                var result = await history.Ask<HistoryCategoriesResult>(new QueryHistoryCategories(), _askTimeout);
                return Results.Ok(result.Categories);
            }
            catch (Exception)
            {
                return ApiResults.GatewayTimeout();
            }
        })
        .WithSummary("List download categories")
        .Produces<string[]>()
        .ProducesProblem(504);

        group.MapDelete("/queue/{id:guid}", async (Guid id, IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            try
            {
                var result = await manager.Ask<DeleteDownloadResult>(new DeleteDownload(id), _askTimeout);
                return result.Success
                    ? Results.Ok(new ApiModels.OperationResult(true))
                    : Results.NotFound(new ApiModels.OperationResult(false, result.Error));
            }
            catch (Exception)
            {
                return ApiResults.GatewayTimeout();
            }
        })
        .WithSummary("Cancel download")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapDelete("/history/{id:guid}", async (Guid id, IActorRegistry registry) =>
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            try
            {
                var result = await history.Ask<DeleteDownloadResult>(new RemoveHistoryEntry(id), _askTimeout);
                return result.Success
                    ? Results.Ok(new ApiModels.OperationResult(true))
                    : Results.NotFound(new ApiModels.OperationResult(false, result.Error));
            }
            catch (Exception)
            {
                return ApiResults.GatewayTimeout();
            }
        })
        .WithSummary("Delete history entry")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        group.MapPost("/{id:guid}/retry", async (Guid id, IActorRegistry registry) =>
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            try
            {
                history.Tell(new RemoveHistoryEntry(id));
                var result = await manager.Ask<RetryDownloadResult>(new RetryDownload(id), _askTimeout);
                return result.Success
                    ? Results.Ok(new ApiModels.OperationResult(true))
                    : Results.BadRequest(new ApiModels.OperationResult(false, result.Error));
            }
            catch (Exception)
            {
                return ApiResults.GatewayTimeout();
            }
        })
        .WithSummary("Retry failed download")
        .Produces<ApiModels.OperationResult>()
        .ProducesProblem(504);

        return app;
    }
}
