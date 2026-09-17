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

public abstract record QueryMediathekResponse;

public sealed record QueryMediathekCompleted(
    MediathekItem[] Items,
    int Total) : QueryMediathekResponse;

public sealed record QueryMediathekFailed(Exception Cause) : QueryMediathekResponse;
