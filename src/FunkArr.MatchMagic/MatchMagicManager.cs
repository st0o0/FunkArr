using Akka.Actor;
using Akka.Routing;
using FunkArr.Core;
using FunkArr.Messages.Scoring;
using Microsoft.Extensions.Options;

namespace FunkArr.MatchMagic;

public sealed class MatchMagicManager : ReceiveActor
{
    private readonly IActorRef _router;
    private MatchMagicManagerState _state = MatchMagicManagerState.Empty;

    public MatchMagicManager(IOptionsMonitor<ScoringOptions> optionsMonitor)
    {
        var routerProps = Props.Create<MatchMagicActor>()
            .WithRouter(new SmallestMailboxPool(optionsMonitor.CurrentValue.PoolSize));
        _router = Context.ActorOf(routerProps, "scoring-pool");

        Receive<MatchingConfig>(config => _state = _state.Apply(config));
        Receive<RemoveMatchingConfig>(msg => _state = _state.Apply(msg));
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

        _router.Tell(new ExecuteScoring(config, msg.Candidates, msg.RequestId, msg.Origin), Sender);
    }
}
