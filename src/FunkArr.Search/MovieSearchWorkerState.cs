using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.MetadataResolver;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record MovieSearchWorkerState(
    Guid SearchId,
    MediathekItem[] RawItems,
    string? RuleSetId,
    string? ImdbId,
    int? TmdbId,
    string? MediaName = null)
{
    public static readonly MovieSearchWorkerState Empty = new(Guid.Empty, [], null, null, null);
}

public static class MovieSearchWorkerStateExtensions
{
    public static MovieSearchWorkerState Apply(this MovieSearchWorkerState state, MovieSearchCommand cmd) =>
        state with { SearchId = cmd.SearchId, ImdbId = cmd.ImdbId, TmdbId = cmd.TmdbId };

    public static MovieSearchWorkerState Apply(this MovieSearchWorkerState state, MediathekQueryCompleted result) =>
        state with { RawItems = result.Items };

    public static MovieSearchWorkerState ApplyRuleSet(this MovieSearchWorkerState state, string ruleSetId, string? mediaName = null) =>
        state with { RuleSetId = ruleSetId, MediaName = mediaName };

    public static SearchCompleted ToUnscoredResult(this MovieSearchWorkerState state)
    {
        var items = state.RawItems
            .SelectMany(raw => ToResultItems(state, raw, 0.0, null))
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    public static SearchCompleted ToScoredResult(
        this MovieSearchWorkerState state, ScoreCompleted scored)
    {
        var items = scored.Results
            .SelectMany(s => ToResultItems(state, state.RawItems[s.Index], s.Score, s.Metadata))
            .OrderByDescending(i => i.Score)
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    public static SearchCompleted ToScoredResult(
        this MovieSearchWorkerState state, ScoreCompleted scored,
        IReadOnlyDictionary<int, MovieResolved> resolvedMovies)
    {
        var items = scored.Results
            .SelectMany(s =>
            {
                var raw = state.RawItems[s.Index];
                resolvedMovies.TryGetValue(s.Index, out var resolved);
                return ToResultItems(state, raw, s.Score, s.Metadata,
                    resolved?.Confidence, resolved?.Strategy,
                    resolved?.ImdbId ?? state.ImdbId, resolved?.TmdbId ?? state.TmdbId);
            })
            .OrderByDescending(i => i.Score)
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    private static SearchResultItem[] ToResultItems(
        MovieSearchWorkerState state, MediathekItem raw, double score, MetadataSpec? metadata,
        float? resolutionConfidence = null, string? resolutionStrategy = null,
        string? imdbId = null, int? tmdbId = null)
    {
        var variants = VideoQuality.GetVariants(raw);
        if (variants.Length == 0)
        {
            return [];
        }

        return variants.Select(v =>
        {
            var title = ReleaseTitleBuilder.Build(state.MediaName ?? raw.Topic, raw.Title, metadata, v.Quality, "movie");

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
                ImdbId: imdbId ?? state.ImdbId,
                TmdbId: tmdbId ?? state.TmdbId,
                ResolutionConfidence: resolutionConfidence,
                ResolutionStrategy: resolutionStrategy);
        }).ToArray();
    }

    public static int ResolveQuality(MediathekItem item) =>
        item.UrlVideoHd is not null ? 1080 :
        item.UrlVideo is not null ? 720 :
        item.UrlVideoLow is not null ? 480 : 0;
}
