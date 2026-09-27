using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FunkArr.Download;

internal sealed class SubtitlePreparer(IHttpClientFactory httpClientFactory, ILogger<SubtitlePreparer> logger) : ISubtitlePreparer
{
    private static ISubtitleFormat[] CreateFormats() => [new TtmlFormat(), new WebVttFormat(), new SrtFormat()];

    public async Task<SubtitleResult> PrepareAsync(string url, string outputDirectory, string routeName, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        string? detectedFormat = null;

        try
        {
            var result = await PrepareInternalAsync(url, outputDirectory, routeName, ct);
            sw.Stop();

            switch (result)
            {
                case SubtitleResult.Succeeded s:
                    detectedFormat = s.Track.Format;
                    Telemetry.SubtitleTotal.Add(1,
                        new KeyValuePair<string, object?>("status", "succeeded"),
                        new KeyValuePair<string, object?>("format", s.Track.Format.ToLowerInvariant()));
                    logger.LogDebug("Subtitle prepared from {Url}, format {Format}", url, s.Track.Format);
                    break;
                case SubtitleResult.Failed f:
                    Telemetry.SubtitleTotal.Add(1,
                        new KeyValuePair<string, object?>("status", "failed"),
                        new KeyValuePair<string, object?>("format", "unknown"));
                    logger.LogWarning("Subtitle preparation failed for {Url}: {Reason} {Detail}", url, f.Reason, f.Detail);
                    break;
                case SubtitleResult.Unavailable:
                    Telemetry.SubtitleTotal.Add(1,
                        new KeyValuePair<string, object?>("status", "unavailable"),
                        new KeyValuePair<string, object?>("format", "unknown"));
                    break;
            }

            Telemetry.SubtitleDuration.Record(sw.Elapsed.TotalSeconds);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Telemetry.SubtitleTotal.Add(1,
                new KeyValuePair<string, object?>("status", "failed"),
                new KeyValuePair<string, object?>("format", "unknown"));
            Telemetry.SubtitleDuration.Record(sw.Elapsed.TotalSeconds);
            logger.LogWarning(ex, "Subtitle preparation failed for {Url}", url);
            return new SubtitleResult.Failed(SubtitleFailureReason.DownloadFailed, ex.Message);
        }
    }

    private async Task<SubtitleResult> PrepareInternalAsync(string url, string outputDirectory, string routeName, CancellationToken ct)
    {
        string content;
        try
        {
            using var client = httpClientFactory.CreateClient($"route:{routeName}");
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new SubtitleResult.Failed(SubtitleFailureReason.DownloadFailed, $"HTTP {(int)response.StatusCode}");

            content = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new SubtitleResult.Failed(SubtitleFailureReason.DownloadFailed, ex.Message);
        }

        if (string.IsNullOrWhiteSpace(content))
            return new SubtitleResult.Failed(SubtitleFailureReason.EmptyContent);

        content = StripBom(content);

        foreach (var format in CreateFormats())
        {
            if (!format.CanParse(content))
                continue;

            var cues = format.Parse(content);
            if (cues.Count == 0)
                return new SubtitleResult.Failed(SubtitleFailureReason.ConversionFailed, $"{format.Name} parsed but produced no cues");

            var srt = SrtEmitter.Emit(cues);
            var path = Path.Combine(outputDirectory, "subtitle.srt");
            await File.WriteAllTextAsync(path, srt, ct);

            var track = new SubtitleTrack(format.Name, "deu", cues);
            return new SubtitleResult.Succeeded(track, path);
        }

        return new SubtitleResult.Failed(SubtitleFailureReason.UnrecognizedFormat);
    }

    private static string StripBom(string content) =>
        content.Length > 0 && content[0] == '﻿' ? content[1..] : content;
}
