using Microsoft.Extensions.Logging;

namespace FunkArr.Download;

internal sealed class SubtitlePreparer(IHttpClientFactory httpClientFactory, ILogger<SubtitlePreparer> logger) : ISubtitlePreparer
{
    public async Task<string?> PrepareAsync(string url, string outputDirectory, string routeName, CancellationToken ct)
    {
        using var activity = Telemetry.Source.StartActivity("download.subtitle");
        string content;
        try
        {
            using var client = httpClientFactory.CreateClient($"route:{routeName}");
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            content = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Failed to download subtitle from {Url}", url);
            return null;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var trimmed = content.TrimStart('﻿').TrimStart();

        if (trimmed.StartsWith("WEBVTT", StringComparison.Ordinal))
        {
            return WriteFile(outputDirectory, ".vtt", content);
        }

        if (trimmed.StartsWith("<?xml", StringComparison.Ordinal) || trimmed.StartsWith("<tt", StringComparison.Ordinal))
        {
            var srt = TtmlToSrtConverter.Convert(content);
            return string.IsNullOrWhiteSpace(srt) ? null : WriteFile(outputDirectory, ".srt", srt);
        }

        if (IsSrt(trimmed))
        {
            return WriteFile(outputDirectory, ".srt", content);
        }

        return null;
    }

    private static bool IsSrt(string content) =>
        content.Length > 5 && char.IsDigit(content[0]) && content.Contains("-->");

    private static string WriteFile(string directory, string extension, string content)
    {
        var path = Path.Combine(directory, $"subtitle{extension}");
        File.WriteAllText(path, content);
        return path;
    }
}
