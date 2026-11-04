using FunkArr.Messages.Mediathek;

namespace FunkArr.Search.Client;

internal sealed record MediathekQueryResult(MediathekItem[] Items, int Total, long ListTimestamp = 0);
