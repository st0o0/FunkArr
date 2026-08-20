using System.Globalization;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Configuration;
using FunkArr.Indexer;
using Microsoft.Extensions.Options;

namespace FunkArr.DownloadClient;

public static class SabnzbdEndpoints
{
    private static readonly TimeSpan AskTimeout = TimeSpan.FromSeconds(10);

    public static void MapSabnzbdEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/download/api");

        group.MapGet("/", HandleSabnzbdGet);
        group.MapPost("/", HandleSabnzbdPost).DisableAntiforgery();
    }

    private static async Task<IResult> HandleSabnzbdGet(
        HttpContext context,
        IOptions<FunkArrOptions> options,
        ActorRegistry actorRegistry)
    {
        if (!ValidateApiKey(context, options.Value))
        {
            return SabnzbdError("API Key Required");
        }

        var mode = context.Request.Query["mode"].FirstOrDefault()?.ToLowerInvariant();
        var queueActor = actorRegistry.Get<DownloadQueueActor>();

        return mode switch
        {
            "version" => Results.Text("4.3.3"),
            "get_config" => HandleGetConfig(options.Value),
            "queue" => await HandleQueue(queueActor, options.Value),
            "history" => await HandleHistory(queueActor, options.Value),
            _ => SabnzbdError($"Unknown mode: {mode}"),
        };
    }

    private static async Task<IResult> HandleSabnzbdPost(
        HttpContext context,
        IOptions<FunkArrOptions> options,
        ActorRegistry actorRegistry)
    {
        if (!ValidateApiKey(context, options.Value))
        {
            return SabnzbdError("API Key Required");
        }

        var mode = context.Request.Query["mode"].FirstOrDefault()?.ToLowerInvariant();

        if (mode == "addfile")
        {
            return await HandleAddFile(context, actorRegistry.Get<DownloadQueueActor>());
        }

        return SabnzbdError($"Unknown mode: {mode}");
    }

    private static IResult HandleGetConfig(FunkArrOptions options)
    {
        var config = new
        {
            misc = new { complete_dir = options.DownloadPath },
        };
        return Results.Json(config);
    }

    private static async Task<IResult> HandleQueue(IActorRef queueActor, FunkArrOptions options)
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

        return Results.Json(new
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

    private static async Task<IResult> HandleHistory(IActorRef queueActor, FunkArrOptions options)
    {
        var response = await queueActor.Ask<DownloadQueueActor.HistoryResponse>(
            new DownloadQueueActor.GetHistory(), AskTimeout);

        var pathMapping = ParsePathMapping(options.PathMapping);

        var slots = response.Jobs.Select(j => new
        {
            nzo_id = j.NzoId,
            name = j.Title,
            status = j.Status == DownloadStatus.Completed ? "Completed" : "Failed",
            storage = MapPath(j.OutputPath ?? string.Empty, pathMapping),
            completed = j.CompletedAt?.ToUnixTimeSeconds() ?? 0,
            fail_message = j.ErrorMessage ?? string.Empty,
        }).ToArray();

        return Results.Json(new
        {
            history = new
            {
                slots,
                noofslots = slots.Length,
            },
        });
    }

    private static async Task<IResult> HandleAddFile(HttpContext context, IActorRef queueActor)
    {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();

        if (file is null)
        {
            return SabnzbdJson(false, "No file uploaded");
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var nzbContent = await reader.ReadToEndAsync();
        var (url, title, subtitleUrl) = FakeNzbBuilder.ParseFakeNzb(nzbContent);

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(title))
        {
            return SabnzbdJson(false, "Could not extract download URL from NZB");
        }

        var nzoId = await queueActor.Ask<string>(
            new DownloadQueueActor.EnqueueDownload(url, title, subtitleUrl), AskTimeout);

        return Results.Json(new
        {
            status = true,
            nzo_ids = new[] { nzoId },
        });
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

    private static bool ValidateApiKey(HttpContext context, FunkArrOptions options)
    {
        var apiKey = context.Request.Query["apikey"].FirstOrDefault();
        return !string.IsNullOrEmpty(apiKey) && apiKey == options.ApiKey;
    }

    private static IResult SabnzbdError(string message) =>
        Results.Json(new { status = false, error = message });

    private static IResult SabnzbdJson(bool status, string? error = null) =>
        Results.Json(new { status, error });
}
