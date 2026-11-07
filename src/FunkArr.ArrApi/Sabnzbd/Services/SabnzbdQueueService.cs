using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Sabnzbd.Models;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Core.Data;
using FunkArr.Messages.Download;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DownloadQueueResponse = FunkArr.Messages.Download.QueueResponse;

namespace FunkArr.ArrApi.Sabnzbd.Services;

public sealed class SabnzbdQueueService(
    IActorRegistry registry,
    IOptions<DownloadOptions> downloadOptions,
    DataPaths dataPaths,
    ILogger<SabnzbdQueueService> logger)
{
    internal async Task<SabnzbdResult<Models.QueueResponse>> GetQueue(int start, int limit, string? category, CancellationToken ct)
    {
        try
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            if (await manager.Ask<DownloadQueueResponse>(
                    new QueryQueue(start, limit, SabnzbdResponseMapper.ParseMediaTypeNullable(category)), ct)
                is not QueueResult result)
            {
                return new SabnzbdError("Queue query failed", 502);
            }

            var slots = result.Items.Select((item, index) =>
                SabnzbdResponseMapper.BuildQueueSlot(item, start + index)).ToArray();

            return new Models.QueueResponse(new QueueData(
                Paused: result.IsPaused,
                Speedlimit: "",
                NoofSlotsTotal: result.TotalItems,
                Diskspace1: "0",
                Diskspace2: "0",
                Speed: "0",
                Slots: slots));
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout querying download queue");
            return new SabnzbdError("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult<HistoryResponse>> GetHistory(int start, int limit, string? category, CancellationToken ct)
    {
        try
        {
            var history = await registry.GetAsync<IDownloadHistoryManager>(ct);
            var response = await history.Ask<HistoryResult>(
                new QueryHistory(start, limit, SabnzbdResponseMapper.ParseMediaTypeNullable(category)), ct);

            var slots = response.Items.Select(item =>
                SabnzbdResponseMapper.BuildHistorySlot(item, dataPaths.Complete)).ToArray();

            return new HistoryResponse(new HistoryData(
                NoofSlots: response.TotalItems,
                Slots: slots));
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout querying download history");
            return new SabnzbdError("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult<FullStatusResponse>> GetFullStatus(CancellationToken ct)
    {
        try
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            if (await manager.Ask<DownloadQueueResponse>(new QueryQueue(), ct) is not QueueResult result)
            {
                return new SabnzbdError("Queue query failed", 502);
            }

            var totalSpeed = result.Items
                .Where(i => i is { Status: DownloadStatus.Processing, Progress.Speed: > 0 })
                .Sum(i => i.TotalBytes > 0 ? i.Progress.BytesDownloaded / Math.Max(1.0, i.Progress.CurrentTimeUs / 1_000_000.0) : 0);

            return new FullStatusResponse(new FullStatusData(
                Paused: result.IsPaused,
                Speedlimit: "",
                Diskspace1: "0",
                Diskspace2: "0",
                Completedir: dataPaths.Complete.Replace('\\', '/'),
                Speed: ((long)totalSpeed).ToString()));
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout querying full status");
            return new SabnzbdError("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult<SabnzbdStatusResponse>> PauseQueue(CancellationToken ct)
    {
        try
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<PauseDownloadsResult>(new PauseDownloads(), ct);
            return result.Success
                ? new SabnzbdStatusResponse(true)
                : new SabnzbdError("Pause failed");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout pausing queue");
            return new SabnzbdError("Request timed out", 504);
        }
    }

    internal async Task<SabnzbdResult<SabnzbdStatusResponse>> ResumeQueue(CancellationToken ct)
    {
        try
        {
            var manager = await registry.GetAsync<IDownloadManager>(ct);
            var result = await manager.Ask<ResumeDownloadsResult>(new ResumeDownloads(), ct);
            return result.Success
                ? new SabnzbdStatusResponse(true)
                : new SabnzbdError("Resume failed");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout resuming queue");
            return new SabnzbdError("Request timed out", 504);
        }
    }

    internal SabnzbdResult<SabnzbdConfigResponse> GetConfig()
    {
        var categories = downloadOptions.Value.Categories
            .Select((c, i) => new SabnzbdCategoryConfig(
                Name: c.Name,
                Order: i,
                Dir: string.IsNullOrEmpty(c.Dir) ? c.Name : c.Dir,
                Newzbin: "",
                Priority: 0))
            .ToArray();

        var tvCategories = downloadOptions.Value.Categories
            .Where(c => c.Name is not ("movie" or "movies"))
            .Select(c => c.Name)
            .ToArray();

        var movieCategories = downloadOptions.Value.Categories
            .Where(c => c.Name is "movie" or "movies")
            .Select(c => c.Name)
            .ToArray();

        return new SabnzbdConfigResponse(new SabnzbdConfig(
            Misc: new SabnzbdMiscConfig(
                CompleteDir: dataPaths.Complete.Replace('\\', '/'),
                EnableTvSorting: false,
                EnableMovieSorting: false,
                EnableDateSorting: false,
                PreCheck: false,
                HistoryRetention: "all",
                HistoryRetentionOption: "all",
                HistoryRetentionNumber: 0,
                TvCategories: tvCategories,
                MovieCategories: movieCategories,
                DateCategories: []),
            Categories: categories,
            Sorters: []));
    }
}
