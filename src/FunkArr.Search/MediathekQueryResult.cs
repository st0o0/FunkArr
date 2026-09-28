using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

internal sealed record MediathekQueryResult(MediathekItem[] Items, int Total);
