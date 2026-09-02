namespace FunkArr.Messages.Mediathek;

public sealed record QueryMediathek(
    MediathekQueryField[] Fields,
    string? SortBy,
    string? SortOrder,
    bool Future,
    int Offset,
    int Size,
    int? DurationMin,
    int? DurationMax);
