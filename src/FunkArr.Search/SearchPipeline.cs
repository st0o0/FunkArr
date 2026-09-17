using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record SearchContext(
    Guid SearchId,
    MediathekItem[] RawItems,
    string? MediaName,
    int? TvdbId,
    string? ImdbId,
    int? TmdbId,
    string MediaType);

public static class SearchPipeline
{
    public static SearchResultItem[] BuildUnscoredItems(SearchContext ctx)
    {
        return ctx.RawItems
            .SelectMany(raw => BuildResultItems(ctx, raw, 0.0, null))
            .ToArray();
    }

    public static SearchResultItem[] BuildScoredItems(SearchContext ctx, ScoreCompleted scored)
    {
        return scored.Results
            .SelectMany(s => BuildResultItems(ctx, ctx.RawItems[s.Index], s.Score, s.Metadata))
            .OrderByDescending(i => i.Score)
            .ToArray();
    }

    public static SearchResultItem[] BuildScoredItems(
        SearchContext ctx, ScoreCompleted scored,
        IReadOnlyDictionary<int, EnrichedEpisode> enrichedEpisodes)
    {
        return scored.Results
            .SelectMany(s =>
            {
                var metadata = s.Metadata;

                if (enrichedEpisodes.TryGetValue(s.Index, out var enriched))
                {
                    metadata = new MetadataSpec(enriched.Season, enriched.Episode, metadata?.AiredAt);
                }

                return BuildResultItems(ctx, ctx.RawItems[s.Index], s.Score, metadata,
                    enrichedEpisodes.TryGetValue(s.Index, out var res) ? res.Confidence : null,
                    enrichedEpisodes.TryGetValue(s.Index, out var resMm) ? resMm.Method : null);
            })
            .OrderByDescending(i => i.Score)
            .ToArray();
    }

    public static SearchResultItem[] BuildScoredItems(
        SearchContext ctx, ScoreCompleted scored,
        IReadOnlyDictionary<int, EnrichedMovie> enrichedMovies)
    {
        return scored.Results
            .SelectMany(s =>
            {
                enrichedMovies.TryGetValue(s.Index, out var enriched);
                return BuildResultItems(ctx, ctx.RawItems[s.Index], s.Score, s.Metadata,
                    enriched?.Confidence, enriched?.Method,
                    enriched?.ImdbId ?? ctx.ImdbId, enriched?.TmdbId ?? ctx.TmdbId);
            })
            .OrderByDescending(i => i.Score)
            .ToArray();
    }

    public static SearchResultItem[] BuildResultItems(
        SearchContext ctx, MediathekItem raw, double score, MetadataSpec? metadata,
        float? matchConfidence = null, MatchMethod? matchMethod = null,
        string? imdbIdOverride = null, int? tmdbIdOverride = null)
    {
        var variants = VideoQuality.GetVariants(raw);
        if (variants.Length == 0)
        {
            return [];
        }

        return variants.Select(v =>
        {
            var title = ReleaseTitleBuilder.Build(
                ctx.MediaName ?? raw.Topic, raw.Title, metadata, v.Quality, ctx.MediaType);

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
                TvdbId: ctx.TvdbId,
                ImdbId: imdbIdOverride ?? ctx.ImdbId,
                TmdbId: tmdbIdOverride ?? ctx.TmdbId,
                Season: metadata?.Season,
                Episode: metadata?.Episode,
                MatchConfidence: matchConfidence,
                MatchMethod: matchMethod);
        }).ToArray();
    }
}
