using Akka.Actor;
using FunkArr.Core.Actors;
using FunkArr.Messages;
using FunkArr.Messages.Search;
using Terp.Akka;

namespace FunkArr.Feed.Eval;

public sealed class FeedEvalActor : ReceiveActor
{
    private static readonly TimeSpan _evaluationTimeout = TimeSpan.FromSeconds(60);

    private sealed record EvalCompleted(EvalGroup Group, SearchResultItem[] Items);
    private sealed record EvalFailed(EvalGroup Group, string Reason);

    private readonly IActorRef _tvEvaluation = Context.GetActor<ITvEvaluationRegion>();
    private readonly IActorRef _movieEvaluation = Context.GetActor<IMovieEvaluationRegion>();
    private IActorRef _replyTo = ActorRefs.Nobody;

    public FeedEvalActor()
    {
        Receive<EvalGroup>(HandleEvalGroup);
        Receive<EvalCompleted>(HandleEvalCompleted);
        Receive<EvalFailed>(HandleEvalFailed);
    }

    private void HandleEvalGroup(EvalGroup group)
    {
        _replyTo = Sender;

        if (group.MediaType == MediaType.Movie)
        {
            _movieEvaluation.Ask<EvaluateMoviesResponse>(
                    new EvaluateMovies(Guid.NewGuid(), SearchSource.Feed,
                        group.Items, group.Ids.ImdbId, group.Ids.TmdbId),
                    _evaluationTimeout)
                .PipeTo(Self,
                    success: response => response switch
                    {
                        EvaluateMoviesCompleted c => new EvalCompleted(group, c.Items),
                        EvaluateMoviesFailed f => new EvalFailed(group, f.Cause.Message),
                        _ => new EvalFailed(group, "unexpected movie evaluation response")
                    },
                    failure: ex => new EvalFailed(group, ex.Message));
        }
        else
        {
            _tvEvaluation.Ask<EvaluateSeriesResponse>(
                    new EvaluateSeries(Guid.NewGuid(), SearchSource.Feed,
                        group.Items, group.Ids.TvdbId, group.Ids.ImdbId),
                    _evaluationTimeout)
                .PipeTo(Self,
                    success: response => response switch
                    {
                        EvaluateSeriesCompleted c => new EvalCompleted(group, c.Items),
                        EvaluateSeriesFailed f => new EvalFailed(group, f.Cause.Message),
                        _ => new EvalFailed(group, "unexpected series evaluation response")
                    },
                    failure: ex => new EvalFailed(group, ex.Message));
        }
    }

    private void HandleEvalCompleted(EvalCompleted result)
    {
        var gate = FeedGate.Evaluate(
            result.Items, result.Group.Items, result.Group.MediaType, result.Group.MinConfidence);

        foreach (var (reason, count) in gate.RejectedByReason)
        {
            Telemetry.Rejected.Add(count, new KeyValuePair<string, object?>("reason", reason.ToString()));
        }

        _replyTo.Tell(new GroupResult(result.Group.RuleSetId, gate.Accepted, gate.RejectedByReason, null));
    }

    private void HandleEvalFailed(EvalFailed result)
    {
        _replyTo.Tell(new GroupResult(result.Group.RuleSetId, [], null, result.Reason));
    }
}
