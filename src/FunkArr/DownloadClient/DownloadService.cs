using FunkArr.Shared;
using Microsoft.Extensions.Logging;

namespace FunkArr.DownloadClient;

public sealed class DownloadService(
    IHttpClientFactory httpClientFactory,
    IFileService fileService,
    ILogger<DownloadService> logger)
{
    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        Action<long, long> onProgress,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient();
        var tempFile = fileService.GetTempVideoPath(request.TempPath, request.NzoId);

        using var response = await client.GetAsync(
            request.VideoUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var downloadedBytes = 0L;
        var buffer = new byte[8192];

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream =
            new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        int bytesRead;
        var lastReport = DateTimeOffset.UtcNow;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            downloadedBytes += bytesRead;

            if (DateTimeOffset.UtcNow - lastReport > TimeSpan.FromSeconds(2))
            {
                onProgress(downloadedBytes, totalBytes);
                lastReport = DateTimeOffset.UtcNow;
            }
        }

        var subtitlePath = await DownloadSubtitleAsync(request, client, cancellationToken);

        return new DownloadResult(tempFile, subtitlePath);
    }

    private async Task<string?> DownloadSubtitleAsync(
        DownloadRequest request, HttpClient client, CancellationToken cancellationToken)
    {
        if (request.SubtitleUrl is null)
        {
            return null;
        }

        var subtitlePath = fileService.GetTempSubtitlePath(request.TempPath, request.NzoId);
        var subResponse = await client.GetAsync(request.SubtitleUrl, cancellationToken);

        if (!subResponse.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Failed to download subtitle for {NzoId}: {Status}", request.NzoId, subResponse.StatusCode);
            return null;
        }

        var subContent = await subResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        await fileService.WriteSubtitleAsync(subtitlePath, subContent);
        return subtitlePath;
    }
}
