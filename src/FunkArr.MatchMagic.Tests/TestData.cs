using System.Reflection;

namespace FunkArr.MatchMagic.Tests;

internal static class TestData
{
    public static string LoadResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"FunkArr.MatchMagic.Tests.Resources.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static MediaItem CreateItem(
        string topic = "Tatort",
        string title = "Tatort: Die goldene Zeit",
        string channel = "ARD",
        int durationMinutes = 90,
        string? urlHd = "https://example.com/hd.mp4",
        string? url = "https://example.com/sd.mp4",
        string? urlLow = "https://example.com/low.mp4",
        long timestamp = 1719244800,
        string? description = null) =>
        new(
            Topic: topic,
            Title: title,
            Description: description,
            Channel: channel,
            Timestamp: timestamp,
            Duration: durationMinutes * 60,
            UrlVideoHd: urlHd,
            UrlVideo: url,
            UrlVideoLow: urlLow);
}
