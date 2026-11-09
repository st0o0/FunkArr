using System.Security.Cryptography;
using System.Text;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;

namespace FunkArr.Feed;

internal sealed record GateResult(FeedEntry[] Accepted, IReadOnlyDictionary<RejectionReason, int> RejectedByReason);

internal static class FeedGate
{

    internal static string StableIdFor(string channel, string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{channel.Trim().ToLowerInvariant()}|{url.Trim()}"));
        return Convert.ToHexStringLower(hash)[..32];
    }

    internal static GateResult Evaluate(
        IEnumerable<SearchResultItem> results,
        IEnumerable<MediathekItem> sources,
        MediaType mediaType,
        double minConfidence)
    {
        var sourceByUrl = new Dictionary<string, MediathekItem>(StringComparer.Ordinal);
        foreach (var source in sources)
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

        foreach (var result in results)
        {
            var reason = Reject(result, mediaType, minConfidence, sourceByUrl, out var source);
            if (reason is not null)
            {
                rejected[reason.Value] = rejected.GetValueOrDefault(reason.Value) + 1;
                continue;
            }

            accepted.Add(new FeedEntry(
                StableIdFor(source!.Channel, result.Url),
                DateTimeOffset.FromUnixTimeSeconds(source.FilmlisteTimestamp),
                mediaType,
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

        return new GateResult([.. accepted], rejected);
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
            MediaType.Movie when result.Metadata?.Ids is not { TmdbId: not null } and not { ImdbId: not null } => RejectionReason.MissingId,
            MediaType.Show when result.Metadata?.Season is null || result.Metadata.Episode is null => RejectionReason.MissingEpisode,
            _ => null
        };
    }
}
