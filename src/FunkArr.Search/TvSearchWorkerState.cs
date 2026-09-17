using Akka.Actor;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record TvSearchWorkerState(
    Guid SearchId,
    IActorRef ReplyTo,
    string Source,
    MediathekItem[] RawItems,
    string? RuleSetId,
    int? TvdbId,
    string? ImdbId,
    int? Season = null,
    string? MediaName = null,
    ScoreCompleted? ScoredResults = null)
{
    public static TvSearchWorkerState From(SearchSeries cmd, IActorRef replyTo) =>
        new(cmd.SearchId, replyTo, cmd.Source, [], null, cmd.TvdbId, cmd.ImdbId, cmd.Season);
}

public static class TvSearchWorkerStateExtensions
{
    public static TvSearchWorkerState Apply(this TvSearchWorkerState state, QueryMediathekCompleted result) =>
        state with { RawItems = result.Items };

    public static TvSearchWorkerState ApplyRuleSet(this TvSearchWorkerState state, string ruleSetId, string? mediaName = null) =>
        state with { RuleSetId = ruleSetId, MediaName = mediaName };

    public static TvSearchWorkerState WithScoredResults(this TvSearchWorkerState state, ScoreCompleted scored) =>
        state with { ScoredResults = scored };

    public static int ResolveQuality(this MediathekItem item) =>
        item.UrlVideoHd is not null ? 1080 :
        item.UrlVideo is not null ? 720 :
        item.UrlVideoLow is not null ? 480 : 0;
}
