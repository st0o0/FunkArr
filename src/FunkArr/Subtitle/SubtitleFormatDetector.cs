using System.Text;

namespace FunkArr.Subtitle;

public static class SubtitleFormatDetector
{
    private const int SniffSize = 512;

    public static SubtitleFormat Detect(byte[] content)
    {
        if (content.Length == 0)
        {
            return SubtitleFormat.Unknown;
        }

        var text = Encoding.UTF8.GetString(content, 0, Math.Min(content.Length, SniffSize));
        return Detect(text);
    }

    public static SubtitleFormat Detect(string content)
    {
        var snippet = content.Length > SniffSize ? content[..SniffSize] : content;
        var trimmed = snippet.TrimStart('﻿').TrimStart();

        if (trimmed.StartsWith("WEBVTT", StringComparison.Ordinal))
        {
            return SubtitleFormat.WebVtt;
        }

        if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("<tt", StringComparison.OrdinalIgnoreCase))
        {
            return SubtitleFormat.Ttml;
        }

        if (trimmed.Contains("-->", StringComparison.Ordinal))
        {
            return SubtitleFormat.Srt;
        }

        return SubtitleFormat.Unknown;
    }

    public static async Task<SubtitleFormat> DetectFromFileAsync(string path)
    {
        var buffer = new byte[SniffSize];
        await using var stream = File.OpenRead(path);
        var bytesRead = await stream.ReadAsync(buffer);
        return Detect(buffer.AsSpan(0, bytesRead).ToArray());
    }
}
