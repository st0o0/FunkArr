using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.MetadataResolver;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record TvSearchWorkerState(
    Guid SearchId,
    MediathekItem[] RawItems,
    string? RuleSetId,
    int? TvdbId,
    string? ImdbId,
    int? Season = null)
{
    public static readonly TvSearchWorkerState Empty = new(Guid.Empty, [], null, null, null);
}

public static class TvSearchWorkerStateExtensions
{
    public static TvSearchWorkerState Apply(this TvSearchWorkerState state, TvSearchCommand cmd) =>
        state with { SearchId = cmd.SearchId, TvdbId = cmd.TvdbId, ImdbId = cmd.ImdbId, Season = cmd.Season };

    public static TvSearchWorkerState Apply(this TvSearchWorkerState state, MediathekQueryCompleted result) =>
        state with { RawItems = result.Items };

    public static TvSearchWorkerState ApplyRuleSet(this TvSearchWorkerState state, string ruleSetId) =>
        state with { RuleSetId = ruleSetId };

    public static SearchCompleted ToUnscoredResult(this TvSearchWorkerState state) =>
        new(state.SearchId, MapItems(state, 0.0), state.RawItems.Length);

    public static SearchCompleted ToScoredResult(
        this TvSearchWorkerState state, Messages.Scoring.ScoreCompleted scored)
    {
        var items = scored.Results
            .Select(s =>
            {
                var raw = state.RawItems[s.Index];
                return ToResultItem(state, raw, s.Score, s.Metadata);
            })
            .OrderByDescending(i => i.Score)
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    public static SearchCompleted ToScoredResult(
        this TvSearchWorkerState state, Messages.Scoring.ScoreCompleted scored,
        IReadOnlyDictionary<int, ResolvedEpisode> resolvedEpisodes)
    {
        var items = scored.Results
            .Select(s =>
            {
                var raw = state.RawItems[s.Index];
                var metadata = s.Metadata;

                if (resolvedEpisodes.TryGetValue(s.Index, out var resolved))
                {
                    metadata = new MetadataSpec(resolved.Season, resolved.Episode, metadata?.AiredAt);
                }

                return ToResultItem(state, raw, s.Score, metadata,
                    resolvedEpisodes.TryGetValue(s.Index, out var res) ? res.Confidence : null,
                    resolvedEpisodes.TryGetValue(s.Index, out var resSt) ? resSt.Strategy : null);
            })
            .OrderByDescending(i => i.Score)
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    private static SearchResultItem[] MapItems(TvSearchWorkerState state, double score) =>
        state.RawItems.Select(raw => ToResultItem(state, raw, score, null)).ToArray();

    private static SearchResultItem ToResultItem(
        TvSearchWorkerState state, MediathekItem raw, double score, MetadataSpec? metadata,
        float? resolutionConfidence = null, string? resolutionStrategy = null)
    {
        var quality = ResolveQuality(raw);
        var title = ReleaseTitleBuilder.Build(raw.Topic, raw.Title, metadata, quality, "tv");

        return new SearchResultItem(
            Title: title,
            Channel: raw.Channel,
            Topic: raw.Topic,
            Url: raw.UrlVideoHd ?? raw.UrlVideo ?? raw.UrlVideoLow ?? "",
            Duration: raw.Duration,
            Size: raw.Size,
            Quality: quality,
            AiredAt: raw.Timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(raw.Timestamp)
                : null,
            Score: score,
            SubtitleUrl: string.IsNullOrEmpty(raw.UrlSubtitle) ? null : raw.UrlSubtitle,
            TvdbId: state.TvdbId,
            ImdbId: state.ImdbId,
            Season: metadata?.Season,
            Episode: metadata?.Episode,
            ResolutionConfidence: resolutionConfidence,
            ResolutionStrategy: resolutionStrategy);
    }

    public static int ResolveQuality(MediathekItem item) =>
        item.UrlVideoHd is not null ? 720 :
        item.UrlVideo is not null ? 480 :
        item.UrlVideoLow is not null ? 270 : 0;
}
