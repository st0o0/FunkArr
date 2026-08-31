namespace FunkArr.Messages.Mediathek;

public sealed record MediathekQueryField(
    string[] Fields,
    string Query);
