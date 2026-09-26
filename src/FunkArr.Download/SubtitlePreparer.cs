using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace FunkArr.Download;

internal sealed class SubtitlePreparer(IHttpClientFactory httpClientFactory, ILogger<SubtitlePreparer> logger) : ISubtitlePreparer
{
    public async Task<string?> PrepareAsync(string url, string outputDirectory, string routeName, CancellationToken ct)
    {
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

        try
        {
            var doc = XDocument.Parse(content);
            if (doc.Root?.Name.LocalName == "tt")
            {
                var srt = TtmlToSrtConverter.Convert(content);
                if (string.IsNullOrWhiteSpace(srt))
                {
                    logger.LogWarning("Subtitle conversion produced empty result for {Url}", url);
                    return null;
                }

                return WriteFile(outputDirectory, ".srt", srt);
            }
        }
        catch (System.Xml.XmlException)
        {
        }

        if (trimmed.StartsWith("WEBVTT", StringComparison.Ordinal))
        {
            return WriteFile(outputDirectory, ".vtt", content);
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
