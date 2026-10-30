namespace FunkArr.Messages.Mediathek;

public sealed record QueryMediathek(
    MediathekQueryField[] Fields,
    string? SortBy,
    string? SortOrder,
    int Offset,
    int Size,
    int? DurationMin,
    int? DurationMax);

public abstract record QueryMediathekResponse;

public sealed record QueryMediathekCompleted(
    MediathekItem[] Items,
    int Total,
    long ListTimestamp = 0) : QueryMediathekResponse;

public abstract record QueryMediathekFailed : QueryMediathekResponse;

public sealed record QueryMediathekQueueFull : QueryMediathekFailed;

public sealed record QueryMediathekError(Exception Cause) : QueryMediathekFailed;
