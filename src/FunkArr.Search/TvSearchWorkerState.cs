using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.MetadataResolver;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record TvSearchWorkerState(
    Guid SearchId,
    string Source,
    MediathekItem[] RawItems,
    string? RuleSetId,
    int? TvdbId,
    string? ImdbId,
    int? Season = null,
    string? MediaName = null)
{
    public static readonly TvSearchWorkerState Empty = new(Guid.Empty, "", [], null, null, null);
}

public static class TvSearchWorkerStateExtensions
{
    public static TvSearchWorkerState Apply(this TvSearchWorkerState state, TvSearchCommand cmd) =>
        state with { SearchId = cmd.SearchId, Source = cmd.Source, TvdbId = cmd.TvdbId, ImdbId = cmd.ImdbId, Season = cmd.Season };

    public static TvSearchWorkerState Apply(this TvSearchWorkerState state, MediathekQueryCompleted result) =>
        state with { RawItems = result.Items };

    public static TvSearchWorkerState ApplyRuleSet(this TvSearchWorkerState state, string ruleSetId, string? mediaName = null) =>
        state with { RuleSetId = ruleSetId, MediaName = mediaName };

    public static SearchCompleted ToUnscoredResult(this TvSearchWorkerState state)
    {
        var items = state.RawItems
            .SelectMany(raw => ToResultItems(state, raw, 0.0, null))
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    public static SearchCompleted ToScoredResult(
        this TvSearchWorkerState state, ScoreCompleted scored)
    {
        var items = scored.Results
            .SelectMany(s => ToResultItems(state, state.RawItems[s.Index], s.Score, s.Metadata))
            .OrderByDescending(i => i.Score)
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    public static SearchCompleted ToScoredResult(
        this TvSearchWorkerState state, ScoreCompleted scored,
        IReadOnlyDictionary<int, ResolvedEpisode> resolvedEpisodes)
    {
        var items = scored.Results
            .SelectMany(s =>
            {
                var raw = state.RawItems[s.Index];
                var metadata = s.Metadata;

                if (resolvedEpisodes.TryGetValue(s.Index, out var resolved))
                {
                    metadata = new MetadataSpec(resolved.Season, resolved.Episode, metadata?.AiredAt);
                }

                return ToResultItems(state, raw, s.Score, metadata,
                    resolvedEpisodes.TryGetValue(s.Index, out var res) ? res.Confidence : null,
                    resolvedEpisodes.TryGetValue(s.Index, out var resSt) ? resSt.Strategy : null);
            })
            .OrderByDescending(i => i.Score)
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    private static SearchResultItem[] ToResultItems(
        TvSearchWorkerState state, MediathekItem raw, double score, MetadataSpec? metadata,
        float? resolutionConfidence = null, string? resolutionStrategy = null)
    {
        var variants = VideoQuality.GetVariants(raw);
        if (variants.Length == 0)
        {
            return [];
        }

        return variants.Select(v =>
        {
            var title = ReleaseTitleBuilder.Build(state.MediaName ?? raw.Topic, raw.Title, metadata, v.Quality, "tv");

            return new SearchResultItem(
                Title: title,
                Channel: raw.Channel,
                Topic: raw.Topic,
                Url: v.Url,
                Duration: raw.Duration,
                Size: raw.Size > 0 ? raw.Size : v.EstimatedSize,
                Quality: v.Quality,
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
        }).ToArray();
    }

    public static int ResolveQuality(this MediathekItem item) =>
        item.UrlVideoHd is not null ? 1080 :
        item.UrlVideo is not null ? 720 :
        item.UrlVideoLow is not null ? 480 : 0;

}
