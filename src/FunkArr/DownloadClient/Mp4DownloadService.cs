using FunkArr.Shared;

namespace FunkArr.DownloadClient;

public sealed class Mp4DownloadService(
    IHttpClientFactory httpClientFactory,
    IFileService fileService)
{
    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress> progress,
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
                progress.Report(new DownloadProgress { DownloadedBytes = downloadedBytes, TotalBytes = totalBytes });
                lastReport = DateTimeOffset.UtcNow;
            }
        }

        return new DownloadResult(tempFile, null);
    }
}
