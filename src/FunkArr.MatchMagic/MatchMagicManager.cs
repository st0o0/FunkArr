using Akka.Actor;
using Akka.Routing;
using FunkArr.Messages.Scoring;

namespace FunkArr.MatchMagic;

public sealed class MatchMagicManager : ReceiveActor
{
    private readonly IActorRef _router;
    private readonly IActorRef _historyRef;
    private MatchMagicManagerState _state = MatchMagicManagerState.Empty;

    public MatchMagicManager(int poolSize = 4, IActorRef? historyRef = null)
    {
        var routerProps = Props.Create<MatchMagicActor>()
            .WithRouter(new SmallestMailboxPool(poolSize));
        _router = Context.ActorOf(routerProps, "scoring-pool");
        _historyRef = historyRef ?? ActorRefs.Nobody;

        Receive<MatchingConfig>(config => _state = _state.Apply(config));
        Receive<ScoreItems>(HandleScoreItems);
    }

    private void HandleScoreItems(ScoreItems msg)
    {
        var config = _state.GetConfig(msg.RuleSetId);
        if (config is null)
        {
            var defaults = msg.Candidates.Select((_, i) => new ScoredItem(i, 0.0, false)).ToArray();
            Sender.Tell(new ScoreCompleted(msg.RequestId, defaults));
            return;
        }

        _router.Tell(new ExecuteScoring(config, msg.Candidates, msg.RequestId, msg.Origin, _historyRef), Sender);
    }
}
