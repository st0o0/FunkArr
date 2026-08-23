using System.Diagnostics;
using System.Text.Json;
using FunkArr.Shared;

namespace FunkArr.Subtitle;

public sealed class SubtitleAcquisitionService(
    IHttpClientFactory httpClientFactory,
    IFileService fileService,
    ILogger<SubtitleAcquisitionService> logger)
{
    private const string FfprobeBinary = "ffprobe";
    private const string FfmpegBinary = "ffmpeg";

    public async Task<string?> AcquireAsync(
        string? subtitleUrl,
        string? hlsManifestUrl,
        string tempPath,
        string nzoId,
        CancellationToken ct = default)
    {
        if (subtitleUrl is not null)
        {
            return await DownloadSubtitleAsync(subtitleUrl, tempPath, nzoId, ct);
        }

        if (hlsManifestUrl is not null)
        {
            return await ExtractFromHlsAsync(hlsManifestUrl, tempPath, nzoId, ct);
        }

        return null;
    }

    private async Task<string?> DownloadSubtitleAsync(
        string url, string tempPath, string nzoId, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to download subtitle for {NzoId}: {Status}", nzoId, response.StatusCode);
                return null;
            }

            var extension = Path.GetExtension(new Uri(url).AbsolutePath);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".sub";
            }

            var subtitlePath = fileService.GetTempSubtitlePath(tempPath, nzoId, extension);
            var content = await response.Content.ReadAsByteArrayAsync(ct);
            await fileService.WriteSubtitleAsync(subtitlePath, content);
            return subtitlePath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Subtitle download failed for {NzoId}", nzoId);
            return null;
        }
    }

    private async Task<string?> ExtractFromHlsAsync(
        string manifestUrl, string tempPath, string nzoId, CancellationToken ct)
    {
        try
        {
            if (!await HasSubtitleStream(manifestUrl, ct))
            {
                return null;
            }

            var outputPath = fileService.GetTempSubtitlePath(tempPath, nzoId, ".srt");
            var args = $"-i \"{manifestUrl}\" -map 0:s:0 -c:s srt -y \"{outputPath}\"";

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = FfmpegBinary,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();
            await process.StandardError.ReadToEndAsync(ct);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(120));
            await process.WaitForExitAsync(cts.Token);

            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                logger.LogWarning("Failed to extract subtitle from HLS for {NzoId}", nzoId);
                return null;
            }

            return outputPath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "HLS subtitle extraction failed for {NzoId}", nzoId);
            return null;
        }
    }

    internal async Task<bool> HasSubtitleStream(string manifestUrl, CancellationToken ct)
    {
        try
        {
            var args = $"-v quiet -print_format json -show_streams \"{manifestUrl}\"";

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = FfprobeBinary,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            await process.WaitForExitAsync(cts.Token);

            if (process.ExitCode != 0)
            {
                return false;
            }

            using var doc = JsonDocument.Parse(output);
            var streams = doc.RootElement.GetProperty("streams");

            foreach (var stream in streams.EnumerateArray())
            {
                if (stream.TryGetProperty("codec_type", out var codecType)
                    && codecType.GetString() == "subtitle")
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "ffprobe failed for {Url}", manifestUrl);
            return false;
        }
    }
}
