using Akka.Actor;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record MovieSearchWorkerState(
    Guid SearchId,
    IActorRef ReplyTo,
    string Source,
    MediathekItem[] RawItems,
    string? RuleSetId,
    string? ImdbId,
    int? TmdbId,
    string? MediaName = null,
    ScoreCompleted? ScoredResults = null)
{
    public static MovieSearchWorkerState From(SearchMovie cmd, IActorRef replyTo) =>
        new(cmd.SearchId, replyTo, cmd.Source, [], null, cmd.ImdbId, cmd.TmdbId);
}

public static class MovieSearchWorkerStateExtensions
{
    public static MovieSearchWorkerState Apply(this MovieSearchWorkerState state, QueryMediathekCompleted result) =>
        state with { RawItems = result.Items };

    public static MovieSearchWorkerState ApplyRuleSet(this MovieSearchWorkerState state, string ruleSetId, string? mediaName = null) =>
        state with { RuleSetId = ruleSetId, MediaName = mediaName };

    public static MovieSearchWorkerState WithScoredResults(this MovieSearchWorkerState state, ScoreCompleted scored) =>
        state with { ScoredResults = scored };
}
