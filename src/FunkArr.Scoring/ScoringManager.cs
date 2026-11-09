using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using FunkArr.Core.Configuration;
using FunkArr.Messages.Scoring;
using Microsoft.Extensions.Options;
using Terp.Diagnostics;

namespace FunkArr.Scoring;

public sealed class ScoringManager : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _router;
    private readonly ScoringManagerState _state = ScoringManagerState.Empty;

    public ScoringManager(IOptionsMonitor<ScoringOptions> optionsMonitor)
    {
        _router = Context.ActorOf(
            Props.Create<ScoringActor>()
                .WithRouter(new SmallestMailboxPool(optionsMonitor.CurrentValue.PoolSize)),
            "scoring-pool");

        Receive<MatchingConfig>(config => _state.Apply(config));
        Receive<RemoveMatchingConfig>(msg => _state.Apply(msg));
        Receive<ScoreItems>(HandleScoreItems);
        Receive<TestScoreItems>(HandleTestScoreItems);
    }

    private void HandleScoreItems(ScoreItems msg)
    {
        var config = _state.GetConfig(msg.RuleSetId);
        if (config is null)
        {
            Telemetry.NoConfig.Up();
            _log.Debug("No matching config for ruleset {RuleSetId}, returning unscored defaults", msg.RuleSetId);
            var defaults = msg.Candidates.Select((_, i) => new ScoredItem(i, 0.0, false)).ToArray();
            Sender.Tell(new ScoreCompleted(msg.RequestId, defaults, []));
            return;
        }

        _router.Tell(new ExecuteScoring(config, msg.Candidates, msg.RequestId), Sender);
    }

    private void HandleTestScoreItems(TestScoreItems msg)
        => _router.Tell(new ExecuteScoring(msg.Config, msg.Candidates, msg.TestScoringId), Sender);
}
