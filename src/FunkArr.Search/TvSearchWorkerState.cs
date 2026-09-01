using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record TvSearchWorkerState(
    Guid SearchId,
    MediathekItem[] RawItems,
    string? RuleSetId)
{
    public static readonly TvSearchWorkerState Empty = new(Guid.Empty, [], null);
}

public static class TvSearchWorkerStateExtensions
{
    public static TvSearchWorkerState Apply(this TvSearchWorkerState state, TvSearchCommand cmd) =>
        state with { SearchId = cmd.SearchId };

    public static TvSearchWorkerState Apply(this TvSearchWorkerState state, MediathekQueryCompleted result) =>
        state with { RawItems = result.Items };

    public static TvSearchWorkerState ApplyRuleSet(this TvSearchWorkerState state, string ruleSetId) =>
        state with { RuleSetId = ruleSetId };

    public static SearchCompleted ToUnscoredResult(this TvSearchWorkerState state) =>
        new(state.SearchId, MapItems(state.RawItems, 0.0), state.RawItems.Length);

    public static SearchCompleted ToScoredResult(
        this TvSearchWorkerState state, Messages.Scoring.ScoreCompleted scored)
    {
        var items = scored.Results
            .Select(s =>
            {
                var raw = state.RawItems[s.Index];
                return ToResultItem(raw, s.Score);
            })
            .OrderByDescending(i => i.Score)
            .ToArray();

        return new SearchCompleted(state.SearchId, items, items.Length);
    }

    private static SearchResultItem[] MapItems(MediathekItem[] items, double score) =>
        items.Select(raw => ToResultItem(raw, score)).ToArray();

    private static SearchResultItem ToResultItem(MediathekItem raw, double score) => new(
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
        Score: score);

    public static int ResolveQuality(MediathekItem item) =>
        item.UrlVideoHd is not null ? 720 :
        item.UrlVideo is not null ? 480 :
        item.UrlVideoLow is not null ? 270 : 0;
}
