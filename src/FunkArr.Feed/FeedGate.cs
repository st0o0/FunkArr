using System.Security.Cryptography;
using System.Text;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;

namespace FunkArr.Feed;

internal sealed record GateResult(FeedEntry[] Accepted, IReadOnlyDictionary<string, int> RejectedByReason);

internal static class FeedGate
{
    internal const string LowConfidence = "low_confidence";
    internal const string MissingEpisode = "missing_episode";
    internal const string MissingId = "missing_id";
    internal const string UnknownSource = "unknown_source";
    internal const string MissingTimestamp = "missing_timestamp";

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
        var rejected = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var result in results)
        {
            var reason = Reject(result, mediaType, minConfidence, sourceByUrl, out var source);
            if (reason is not null)
            {
                rejected[reason] = rejected.GetValueOrDefault(reason) + 1;
                continue;
            }

            accepted.Add(new FeedEntry(
                StableIdFor(source!.Channel, result.Url),
                DateTimeOffset.FromUnixTimeSeconds(source.FilmlisteTimestamp),
                mediaType,
                result));
        }

        return new GateResult([.. accepted], rejected);
    }

    private static string? Reject(
        SearchResultItem result,
        MediaType mediaType,
        double minConfidence,
        Dictionary<string, MediathekItem> sourceByUrl,
        out MediathekItem? source)
    {
        if (!sourceByUrl.TryGetValue(result.Url, out source))
        {
            return UnknownSource;
        }

        if (source.FilmlisteTimestamp <= 0)
        {
            return MissingTimestamp;
        }

        if (result.Score < minConfidence)
        {
            return LowConfidence;
        }

        return mediaType switch
        {
            MediaType.Movie when result.Metadata?.Ids is not { TmdbId: not null } and not { ImdbId: not null } => MissingId,
            MediaType.Show when result.Metadata?.Season is null || result.Metadata.Episode is null => MissingEpisode,
            _ => null
        };
    }
}
