using FunkArr.Messages.Mediathek;

namespace FunkArr.Feed.Page;

internal sealed record FetchPage(int Offset, CrawlWindow Window, int PageSize);

internal sealed record PageResult(int Offset, MediathekItem[] Items, bool ReachedEnd, string? Error);
