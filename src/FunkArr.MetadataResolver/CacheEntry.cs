using System.Globalization;
using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

internal sealed record EpisodeCacheEntry(
    TvdbEpisode[] Episodes,
    ResolvedEpisode[] Resolved,
    DateTimeOffset FetchedAt,
    TimeSpan Ttl)
{
    public bool IsExpired => DateTimeOffset.UtcNow - FetchedAt > Ttl;
}

internal sealed record MovieCacheEntry(
    MovieResolved[] Resolved,
    DateTimeOffset FetchedAt,
    TimeSpan Ttl)
{
    public bool IsExpired => DateTimeOffset.UtcNow - FetchedAt > Ttl;
}

internal static class CacheTtl
{
    public static readonly TimeSpan ActiveShow = TimeSpan.FromDays(2);
    public static readonly TimeSpan InactiveShow = TimeSpan.FromDays(7);
    public static readonly TimeSpan Movie = TimeSpan.FromDays(30);

    public static TimeSpan DetermineShowTtl(TvdbEpisode[] episodes)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hasUpcoming = episodes.Any(e =>
            e.Aired is not null &&
            DateOnly.TryParseExact(e.Aired, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d) &&
            d > today);
        return hasUpcoming ? ActiveShow : InactiveShow;
    }
}
