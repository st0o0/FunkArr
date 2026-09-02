using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record MovieSearchWorkerState(
    Guid SearchId,
    MediathekItem[] RawItems,
    string? RuleSetId,
    string? ImdbId,
    int? TmdbId)
{
    public static readonly MovieSearchWorkerState Empty = new(Guid.Empty, [], null, null, null);
}

public static class MovieSearchWorkerStateExtensions
{
    public static MovieSearchWorkerState Apply(this MovieSearchWorkerState state, MovieSearchCommand cmd) =>
        state with { SearchId = cmd.SearchId, ImdbId = cmd.ImdbId, TmdbId = cmd.TmdbId };

    public static MovieSearchWorkerState Apply(this MovieSearchWorkerState state, MediathekQueryCompleted result) =>
        state with { RawItems = result.Items };

    public static MovieSearchWorkerState ApplyRuleSet(this MovieSearchWorkerState state, string ruleSetId) =>
        state with { RuleSetId = ruleSetId };

    public static SearchCompleted ToUnscoredResult(this MovieSearchWorkerState state) =>
        new(state.SearchId, MapItems(state, 0.0), state.RawItems.Length);

    public static SearchCompleted ToScoredResult(
        this MovieSearchWorkerState state, Messages.Scoring.ScoreCompleted scored)
    {
        var items = scored.Results
            .Select(s =>
            {
                var raw = state.RawItems[s.Index];
                return ToResultItem(state, raw, s.Score);
            })
            .OrderByDescending(i => i.Score)
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    private static SearchResultItem[] MapItems(MovieSearchWorkerState state, double score) =>
        state.RawItems.Select(raw => ToResultItem(state, raw, score)).ToArray();

    private static SearchResultItem ToResultItem(MovieSearchWorkerState state, MediathekItem raw, double score) => new(
        Title: raw.Title,
        Channel: raw.Channel,
        Topic: raw.Topic,
        Url: raw.UrlVideoHd ?? raw.UrlVideo ?? raw.UrlVideoLow ?? "",
        Duration: raw.Duration,
        Size: raw.Size,
        Quality: ResolveQuality(raw),
        AiredAt: raw.Timestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(raw.Timestamp)
            : null,
        Score: score,
        ImdbId: state.ImdbId,
        TmdbId: state.TmdbId);

    public static int ResolveQuality(MediathekItem item) =>
        item.UrlVideoHd is not null ? 720 :
        item.UrlVideo is not null ? 480 :
        item.UrlVideoLow is not null ? 270 : 0;
}
