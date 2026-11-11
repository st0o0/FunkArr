using System.Security.Cryptography;
using System.Text;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Crawl;

internal sealed record ScoredGroup(ItemGroup Source, SearchResultItem[] Results)
{
    public GatedResult Gate(double minConfidence)
    {
        var sourceByUrl = new Dictionary<string, MediathekItem>(StringComparer.Ordinal);
        foreach (var source in Source.Items)
        {
            foreach (var url in new[] { source.UrlVideoHd, source.UrlVideo, source.UrlVideoLow })
            {
                if (url is not null)
                {
                    sourceByUrl.TryAdd(url, source);
                }
            }
        }

        var accepted = new List<FeedEntry>();
        var rejected = new Dictionary<RejectionReason, int>();

        foreach (var result in Results)
        {
            var reason = Reject(result, Source.MediaType, minConfidence, sourceByUrl, out var item);
            if (reason is not null)
            {
                rejected[reason.Value] = rejected.GetValueOrDefault(reason.Value) + 1;
                continue;
            }

            accepted.Add(new FeedEntry(
                StableIdFor(item!.Channel, result.Url),
                DateTimeOffset.FromUnixTimeSeconds(item.FilmlisteTimestamp),
                Source.MediaType,
                result.Title,
                result.Channel,
                result.Topic,
                result.Url,
                result.Duration,
                result.Size,
                result.Quality,
                result.AiredAt,
                result.Score,
                result.SubtitleUrl,
                result.Metadata?.Ids,
                result.Metadata?.Season,
                result.Metadata?.Episode));
        }

        return new GatedResult([.. accepted], Source.Items.Length, 0, rejected);
    }

    private static RejectionReason? Reject(
        SearchResultItem result,
        MediaType mediaType,
        double minConfidence,
        Dictionary<string, MediathekItem> sourceByUrl,
        out MediathekItem? source)
    {
        if (!sourceByUrl.TryGetValue(result.Url, out source))
        {
            return RejectionReason.UnknownSource;
        }

        if (source.FilmlisteTimestamp <= 0)
        {
            return RejectionReason.MissingTimestamp;
        }

        if (result.Score < minConfidence)
        {
            return RejectionReason.LowConfidence;
        }

        return mediaType switch
        {
            MediaType.Movie when result.Metadata?.Ids is not { TmdbId: not null } and not { ImdbId: not null } =>
                RejectionReason.MissingId,
            MediaType.Show when result.Metadata?.Season is null || result.Metadata.Episode is null =>
                RejectionReason.MissingEpisode,
            _ => null
        };
    }

    private static string StableIdFor(string channel, string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{channel.Trim().ToLowerInvariant()}|{url.Trim()}"));
        return Convert.ToHexStringLower(hash)[..32];
    }
}
