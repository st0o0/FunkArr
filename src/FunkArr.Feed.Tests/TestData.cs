using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Tests;

internal static class TestData
{
    internal static MediathekItem Item(
        string title = "Herz aus Eis",
        string topic = "Tatort",
        long filmlisteTimestamp = 1_761_800_000,
        string channel = "ARD",
        string? url = null) =>
        new(channel, topic, title, null, 1_761_700_000, 5400, 0,
            null, url ?? $"https://example.com/{Uri.EscapeDataString(title)}-{filmlisteTimestamp}.mp4", null, null, null,
            FilmlisteTimestamp: filmlisteTimestamp);

    internal static SearchResultItem Result(
        string url,
        double score = 0.9,
        string? season = "2026",
        string? episode = "18",
        int? tmdbId = null,
        string? imdbId = null,
        string title = "Tatort.S2026E18.Herz.aus.Eis.GERMAN.720p.WEB.h264-FunkArr") =>
        new(title, "ARD", "Tatort", url, 5400, 1_000_000, 720, null, score, null,
            new MatchMetadata(new ExternalIds(null, imdbId, tmdbId), season, episode, null, null));

    internal static FeedEntry Entry(string stableId, DateTimeOffset firstSeen, MediaType mediaType = MediaType.Show) =>
        new(stableId, firstSeen, mediaType, Result($"https://example.com/{stableId}.mp4"));
}
