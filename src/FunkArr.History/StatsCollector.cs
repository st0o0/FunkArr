using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using Servus.Akka;

namespace FunkArr.History;

public sealed class StatsCollector : ReceiveActor
{
    private static readonly TimeSpan _backfillTimeout = TimeSpan.FromSeconds(5);

    private StatsCollectorState _state = StatsCollectorState.Empty;

    public StatsCollector()
    {
        Receive<UpdateStats>(msg => _state = _state.Apply(msg));
        Receive<QueryAllStats>(_ => Sender.Tell(_state.GetSnapshot()));
        Receive<RemoveStats>(msg => _state = _state.Apply(msg));
        Receive<RegisteredRuleSetsResult>(HandleBackfillRuleSets);
        Receive<BackfillStatsResult>(msg =>
        {
            if (msg.Stats is not null)
            {
                _state = _state.Apply(new UpdateStats(msg.RuleSetId, msg.Stats));
            }
        });
        Receive<BackfillFailed>(_ => { });
    }

    private void HandleBackfillRuleSets(RegisteredRuleSetsResult result)
    {
        var historyRegion = Context.GetActor<IHistoryRegion>();

        foreach (var entry in result.Entries)
        {
            historyRegion.Ask<ScoringStatsResult>(
                    new QueryScoringStats(entry.RuleSetId), _backfillTimeout)
                .PipeTo(Self,
                    success: stats => new BackfillStatsResult(entry.RuleSetId, stats),
                    failure: _ => new BackfillStatsResult(entry.RuleSetId, null));
        }
    }

    private sealed record BackfillFailed;
    private sealed record BackfillStatsResult(string RuleSetId, ScoringStatsResult? Stats);

    protected override void PreStart()
    {
        base.PreStart();

        var resolver = Context.GetActor<IRuleSetResolver>();
        resolver.Ask<RegisteredRuleSetsResult>(
                new QueryRegisteredRuleSets(), _backfillTimeout)
            .PipeTo(Self, failure: _ => new BackfillFailed());
    }
}
