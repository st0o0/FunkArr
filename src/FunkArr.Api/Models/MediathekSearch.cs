namespace FunkArr.Api.Models;

public sealed record MediathekSearchResponse(
    MediathekSearchResult[] Items,
    int TotalResults);

public sealed record MediathekSearchResult(
    string Title,
    string Topic,
    string Channel,
    int Duration,
    int Quality,
    string? Description,
    long Timestamp,
    long Size,
    bool HasSubtitles,
    bool HasHd,
    string? WebsiteUrl);
