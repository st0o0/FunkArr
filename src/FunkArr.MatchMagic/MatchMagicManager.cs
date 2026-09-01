using Akka.Actor;
using Akka.Routing;
using FunkArr.Messages.Scoring;

namespace FunkArr.MatchMagic;

public sealed class MatchMagicManager : ReceiveActor
{
    private readonly Dictionary<string, MatchingConfig> _configs = new(StringComparer.Ordinal);
    private readonly IActorRef _router;
    private readonly IActorRef _historyRef;

    public MatchMagicManager(int poolSize = 4, IActorRef? historyRef = null)
    {
        var routerProps = Props.Create<MatchMagicActor>()
            .WithRouter(new SmallestMailboxPool(poolSize));
        _router = Context.ActorOf(routerProps, "scoring-pool");
        _historyRef = historyRef ?? ActorRefs.Nobody;

        Receive<MatchingConfig>(HandleConfig);
        Receive<ScoreItems>(HandleScoreItems);
    }

    private void HandleConfig(MatchingConfig config)
    {
        _configs[config.RuleSetId] = config;
    }

    private void HandleScoreItems(ScoreItems msg)
    {
        if (!_configs.TryGetValue(msg.RuleSetId, out var config))
        {
            var defaults = msg.Candidates.Select((_, i) => new ScoredItem(i, 0.0, false)).ToArray();
            Sender.Tell(new ScoreCompleted(msg.RequestId, defaults));
            return;
        }

        _router.Tell(new ExecuteScoring(config, msg.Candidates, msg.RequestId, msg.Origin, _historyRef), Sender);
    }
}
