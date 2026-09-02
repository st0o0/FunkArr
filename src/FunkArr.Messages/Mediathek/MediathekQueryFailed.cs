namespace FunkArr.Messages.Mediathek;

public sealed record MediathekQueryFailed(string Reason) : IMediathekResponse;
