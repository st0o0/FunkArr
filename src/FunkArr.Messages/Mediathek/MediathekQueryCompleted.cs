namespace FunkArr.Messages.Mediathek;

public sealed record MediathekQueryCompleted(
    MediathekItem[] Items,
    int Total) : IMediathekResponse;
