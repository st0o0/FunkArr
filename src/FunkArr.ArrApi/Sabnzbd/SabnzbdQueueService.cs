using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Sabnzbd.Models;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DownloadQueueResponse = FunkArr.Messages.Download.QueueResponse;

namespace FunkArr.ArrApi.Sabnzbd;

public sealed class SabnzbdQueueService(
    IActorRegistry registry,
    IOptions<ArrApiOptions> arrApiOptions,
    IOptions<DownloadOptions> downloadOptions,
    DataPaths dataPaths,
    ILogger<SabnzbdQueueService> logger)
{
    private TimeSpan Timeout => TimeSpan.FromSeconds(arrApiOptions.Value.DownloadTimeoutSeconds);

    internal async Task<SabnzbdResult> GetQueue(int start, int limit, string? category)
    {
        try
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            if (await manager.Ask<DownloadQueueResponse>(
                    new QueryQueue(start, limit, SabnzbdResponseMapper.ParseMediaTypeNullable(category)), Timeout)
                is not QueueResult result)
            {
                return new SabnzbdResult.Error("Queue query failed", 502);
            }

            var slots = result.Items.Select((item, index) =>
                SabnzbdResponseMapper.BuildQueueSlot(item, start + index)).ToArray();

            return new SabnzbdResult.Ok(new Models.QueueResponse(new QueueData(
                Paused: result.IsPaused,
                Speedlimit: "",
                NoofSlotsTotal: result.TotalItems,
                Diskspace1: "0",
                Diskspace2: "0",
                Speed: "0",
                Slots: slots)));
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout querying download queue");
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult> GetHistory(int start, int limit, string? category)
    {
        try
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>();
            var response = await history.Ask<HistoryResult>(
                new QueryHistory(start, limit, SabnzbdResponseMapper.ParseMediaTypeNullable(category)), Timeout);

            var slots = response.Items.Select(item =>
                SabnzbdResponseMapper.BuildHistorySlot(item, dataPaths.Complete)).ToArray();

            return new SabnzbdResult.Ok(new HistoryResponse(new HistoryData(
                NoofSlots: response.TotalItems,
                Slots: slots)));
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout querying download history");
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult> GetFullStatus()
    {
        try
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            if (await manager.Ask<DownloadQueueResponse>(new QueryQueue(), Timeout) is not QueueResult result)
            {
                return new SabnzbdResult.Error("Queue query failed", 502);
            }

            var totalSpeed = result.Items
                .Where(i => i is { Status: DownloadStatus.Processing, Speed: > 0 })
                .Sum(i => i.TotalBytes > 0 ? i.BytesDownloaded / Math.Max(1.0, i.CurrentTimeUs / 1_000_000.0) : 0);

            return new SabnzbdResult.Ok(new FullStatusResponse(new FullStatusData(
                Paused: result.IsPaused,
                Speedlimit: "",
                Diskspace1: "0",
                Diskspace2: "0",
                Completedir: dataPaths.Complete.Replace('\\', '/'),
                Speed: ((long)totalSpeed).ToString())));
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout querying full status");
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult> PauseQueue()
    {
        try
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<PauseDownloadsResult>(new PauseDownloads(), Timeout);
            return result.Success
                ? new SabnzbdResult.Ok(new { status = true })
                : new SabnzbdResult.Error("Pause failed");
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout pausing queue");
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult> ResumeQueue()
    {
        try
        {
            var manager = await registry.GetAsync<IDownloadManager>();
            var result = await manager.Ask<ResumeDownloadsResult>(new ResumeDownloads(), Timeout);
            return result.Success
                ? new SabnzbdResult.Ok(new { status = true })
                : new SabnzbdResult.Error("Resume failed");
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Timeout resuming queue");
            return new SabnzbdResult.Error("Request timed out", 504);
        }
    }

    internal SabnzbdResult GetConfig()
    {
        var opts = downloadOptions.Value;
        return new SabnzbdResult.Ok(new
        {
            config = new
            {
                misc = new
                {
                    complete_dir = dataPaths.Complete.Replace('\\', '/'),
                    enable_tv_sorting = false,
                    enable_movie_sorting = false,
                    enable_date_sorting = false,
                    pre_check = false,
                    history_retention = "all",
                    tv_categories = Array.Empty<string>(),
                    movie_categories = Array.Empty<string>(),
                    date_categories = Array.Empty<string>(),
                },
                categories = opts.Categories.Select((c, i) => new
                {
                    name = c.Name,
                    order = i,
                    dir = string.IsNullOrEmpty(c.Dir) ? c.Name : c.Dir,
                    newzbin = "",
                    priority = 0,
                }).ToArray(),
                sorters = Array.Empty<object>(),
            },
        });
    }
}
