namespace FunkArr.Messages.Mediathek;

public sealed record MediathekQuery(
    MediathekQueryField[] Fields,
    string? SortBy,
    string? SortOrder,
    bool Future,
    int Offset,
    int Size,
    int? DurationMin,
    int? DurationMax);
