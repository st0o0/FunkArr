using Akka.Actor;
using Akka.Routing;
using FunkArr.Messages.Scoring;

namespace FunkArr.MatchMagic;

public sealed class MatchMagicManager : ReceiveActor
{
    private readonly Dictionary<string, MatchingConfig> _configs = new(StringComparer.Ordinal);
    private readonly IActorRef _router;

    public MatchMagicManager(int poolSize = 4)
    {
        var routerProps = Props.Create<MatchMagicActor>()
            .WithRouter(new SmallestMailboxPool(poolSize));
        _router = Context.ActorOf(routerProps, "scoring-pool");

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
            var defaults = msg.Items.Select((_, i) => new ScoredItem(i, 0.0, false)).ToArray();
            Sender.Tell(new ScoreCompleted(defaults));
            return;
        }

        _router.Tell(new ExecuteScoring(config, msg.Items), Sender);
    }
}
