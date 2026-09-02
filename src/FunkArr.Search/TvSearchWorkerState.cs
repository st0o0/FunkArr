using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record TvSearchWorkerState(
    Guid SearchId,
    MediathekItem[] RawItems,
    string? RuleSetId,
    int? TvdbId,
    string? ImdbId)
{
    public static readonly TvSearchWorkerState Empty = new(Guid.Empty, [], null, null, null);
}

public static class TvSearchWorkerStateExtensions
{
    public static TvSearchWorkerState Apply(this TvSearchWorkerState state, TvSearchCommand cmd) =>
        state with { SearchId = cmd.SearchId, TvdbId = cmd.TvdbId, ImdbId = cmd.ImdbId };

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
                return ToResultItem(state, raw, s.Score);
            })
            .OrderByDescending(i => i.Score)
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    private static SearchResultItem[] MapItems(TvSearchWorkerState state, double score) =>
        state.RawItems.Select(raw => ToResultItem(state, raw, score)).ToArray();

    private static SearchResultItem ToResultItem(TvSearchWorkerState state, MediathekItem raw, double score) => new(
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
        SubtitleUrl: string.IsNullOrEmpty(raw.UrlSubtitle) ? null : raw.UrlSubtitle,
        TvdbId: state.TvdbId,
        ImdbId: state.ImdbId);

    public static int ResolveQuality(MediathekItem item) =>
        item.UrlVideoHd is not null ? 720 :
        item.UrlVideo is not null ? 480 :
        item.UrlVideoLow is not null ? 270 : 0;
}
