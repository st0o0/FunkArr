using Akka.Actor;
using Akka.Hosting;
using FunkArr.Configuration;
using Microsoft.Extensions.Options;

namespace FunkArr.DownloadClient;

public static class QueueEndpoints
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    public static void MapQueueEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .AddEndpointFilter<QueueApiKeyFilter>();

        group.MapGet("/queue", HandleQueue);
        group.MapGet("/history", HandleHistory);
    }

    private static async Task<IResult> HandleQueue(
        ActorRegistry actorRegistry,
        IOptions<FunkArrOptions> options)
    {
        var queueActor = actorRegistry.Get<DownloadQueueActor>();
        var response = await queueActor.Ask<DownloadQueueActor.QueueResponse>(
            new DownloadQueueActor.GetQueue(), AskTimeout);

        var jobs = response.Jobs
            .Where(j => j.Status is DownloadStatus.Queued or DownloadStatus.Downloading or DownloadStatus.Muxing)
            .Select(j => new
            {
                nzoId = j.NzoId,
                title = j.Title,
                status = j.Status.ToString(),
                progressPercent = j.ProgressPercent,
                downloadedBytes = j.DownloadedBytes,
                totalBytes = j.TotalBytes,
                enqueuedAt = j.EnqueuedAt,
            })
            .ToArray();

        return Results.Json(jobs);
    }

    private static async Task<IResult> HandleHistory(
        ActorRegistry actorRegistry,
        IOptions<FunkArrOptions> options)
    {
        var queueActor = actorRegistry.Get<DownloadQueueActor>();
        var response = await queueActor.Ask<DownloadQueueActor.HistoryResponse>(
            new DownloadQueueActor.GetHistory(), AskTimeout);

        var pathMapping = ParsePathMapping(options.Value.PathMapping);

        var jobs = response.Jobs.Select(j => new
        {
            nzoId = j.NzoId,
            title = j.Title,
            status = j.Status.ToString(),
            outputPath = MapPath(j.OutputPath ?? string.Empty, pathMapping),
            errorMessage = j.ErrorMessage,
            enqueuedAt = j.EnqueuedAt,
            completedAt = j.CompletedAt,
        }).ToArray();

        return Results.Json(jobs);
    }

    private static (string from, string to)? ParsePathMapping(string? mapping)
    {
        if (string.IsNullOrEmpty(mapping))
        {
            return null;
        }

        var parts = mapping.Split(':');
        return parts.Length == 2 ? (parts[0], parts[1]) : null;
    }

    private static string MapPath(string path, (string from, string to)? mapping)
    {
        if (mapping is null || string.IsNullOrEmpty(path))
        {
            return path;
        }

        return path.StartsWith(mapping.Value.from, StringComparison.OrdinalIgnoreCase)
            ? mapping.Value.to + path[mapping.Value.from.Length..]
            : path;
    }
}

public sealed class QueueApiKeyFilter(IOptions<FunkArrOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var apiKey = context.HttpContext.Request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || apiKey != options.Value.ApiKey)
        {
            return Results.Json(new { error = "Incorrect user credentials" }, statusCode: 401);
        }

        return await next(context);
    }
}
