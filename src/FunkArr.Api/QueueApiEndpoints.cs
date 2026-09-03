using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class QueueApiEndpoints
{
    private static readonly TimeSpan _askTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _sseInterval = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public static WebApplication MapQueueApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/downloads");

        group.MapGet("/queue", async (IActorRegistry registry) =>
        {
            var manager = registry.Get<IDownloadManager>();
            try
            {
                var result = await manager.Ask<QueueResult>(new QueryQueue(), _askTimeout);
                return Results.Ok(ToQueueResponse(result));
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        })
        .Produces<ApiModels.DownloadQueueResponse>()
        .ProducesProblem(504);

        group.MapGet("/queue/stream", async (HttpContext ctx, IActorRegistry registry) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            var manager = registry.Get<IDownloadManager>();
            var ct = ctx.RequestAborted;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await manager.Ask<QueueResult>(new QueryQueue(), _askTimeout, ct);
                    var response = ToQueueResponse(result);
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
        });

        group.MapGet("/history", async (int? start, int? limit, string? category, IActorRegistry registry) =>
        {
            var history = registry.Get<IDownloadHistoryManager>();
            try
            {
                var result = await history.Ask<HistoryResult>(
                    new QueryHistory(start ?? 0, limit ?? 25, category), _askTimeout);
                return Results.Ok(ToHistoryResponse(result));
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        })
        .Produces<ApiModels.DownloadHistoryResponse>()
        .ProducesProblem(504);

        group.MapDelete("/queue/{id:guid}", async (Guid id, IActorRegistry registry) =>
        {
            var manager = registry.Get<IDownloadManager>();
            try
            {
                var result = await manager.Ask<DeleteDownloadResult>(new DeleteDownload(id), _askTimeout);
                return result.Success
                    ? Results.Ok(new { success = true })
                    : Results.NotFound(new { success = false, error = result.Error });
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        });

        group.MapDelete("/history/{id:guid}", async (Guid id, IActorRegistry registry) =>
        {
            var history = registry.Get<IDownloadHistoryManager>();
            try
            {
                var result = await history.Ask<DeleteDownloadResult>(new RemoveHistoryEntry(id), _askTimeout);
                return result.Success
                    ? Results.Ok(new { success = true })
                    : Results.NotFound(new { success = false, error = result.Error });
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        });

        group.MapPost("/{id:guid}/retry", async (Guid id, IActorRegistry registry) =>
        {
            var manager = registry.Get<IDownloadManager>();
            var history = registry.Get<IDownloadHistoryManager>();
            try
            {
                history.Tell(new RemoveHistoryEntry(id));
                var result = await manager.Ask<RetryDownloadResult>(new RetryDownload(id), _askTimeout);
                return result.Success
                    ? Results.Ok(new { success = true })
                    : Results.BadRequest(new { success = false, error = result.Error });
            }
            catch (Exception)
            {
                return GatewayTimeout();
            }
        });

        return app;
    }

    internal static ApiModels.DownloadQueueResponse ToQueueResponse(QueueResult result) =>
        new(result.Items.Select(ToQueueItem).ToArray(), result.TotalSlots);

    internal static ApiModels.DownloadQueueItem ToQueueItem(QueueItem item)
    {
        var status = item.Status == DownloadStatus.Processing ? "Processing" : "Queued";
        var percentage = item.TotalDuration > 0
            ? Math.Clamp((int)(item.CurrentTimeUs / 1_000_000.0 / item.TotalDuration * 100), 0, 100)
            : 0;

        var elapsedSeconds = item.CurrentTimeUs / 1_000_000.0;
        var speed = elapsedSeconds > 0 ? (long)(item.BytesDownloaded / elapsedSeconds) : 0;

        var eta = "00:00:00";
        if (speed > 0 && item.TotalBytes > item.BytesDownloaded)
        {
            var remainingSeconds = (item.TotalBytes - item.BytesDownloaded) / (double)speed;
            var ts = TimeSpan.FromSeconds(Math.Min(remainingSeconds, 359999));
            eta = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        return new ApiModels.DownloadQueueItem(
            item.DownloadId.ToString(),
            item.Title,
            status,
            item.Category,
            item.TotalBytes,
            item.BytesDownloaded,
            percentage,
            speed,
            eta);
    }

    internal static ApiModels.DownloadHistoryResponse ToHistoryResponse(HistoryResult result) =>
        new(result.Items.Select(ToHistoryItem).ToArray(), result.TotalItems);

    internal static ApiModels.DownloadHistoryItem ToHistoryItem(HistoryItem item) =>
        new(item.DownloadId.ToString(),
            item.Title,
            item.Category,
            item.TotalBytes,
            item.DownloadTimeSeconds,
            item.Status == DownloadStatus.Completed ? item.FilePath : null,
            item.Status == DownloadStatus.Completed ? "Completed" : "Failed",
            item.Status == DownloadStatus.Failed ? item.FailMessage : null,
            DateTimeOffset.FromUnixTimeSeconds(item.CompletedAt).ToString("o"));

    private static IResult GatewayTimeout() =>
        Results.Problem(statusCode: 504, title: "Gateway Timeout");
}
