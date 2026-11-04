using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using FunkArr.Core.Actors;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using Terp.Akka;

namespace FunkArr.History;

public sealed class StatsCollector : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IMaterializer _materializer = Context.Materializer();

    private readonly StatsCollectorState _state = StatsCollectorState.Empty;

    private static readonly TimeSpan _backfillTimeout = TimeSpan.FromSeconds(5);

    public StatsCollector()
    {
        Receive<UpdateStats>(msg => _state.Apply(msg));
        Receive<QueryAllStats>(_ => Sender.Tell(_state.GetSnapshot()));
        Receive<RemoveStats>(msg => _state.Apply(msg));
        Receive<RegisteredRuleSetsResult>(HandleBackfillRuleSets);
        Receive<BackfillComplete>(HandleBackfillComplete);
        Receive<BackfillFailed>(msg => _log.Error(msg.Cause, ""));
    }

    private void HandleBackfillRuleSets(RegisteredRuleSetsResult result)
    {
        Source.From(result.Entries)
            .Select(e => new QueryScoringStats(e.RuleSetId))
            .Ask<ScoringStatsQueryResult>(Context.GetActor<IHistoryRegion>(), _backfillTimeout, 4)
            .ResumeOnError()
            .RunWith(Sink.Seq<ScoringStatsQueryResult>(), _materializer)
            .PipeTo(Self,
                success: items => new BackfillComplete(items),
                failure: ex => new BackfillFailed(ex));
    }

    private void HandleBackfillComplete(BackfillComplete msg)
    {
        foreach (var item in msg.Results)
        {
            _state.Apply(new UpdateStats(item.RuleSetId, item.Stats));
        }
    }

    private sealed record BackfillFailed(Exception Cause);

    private sealed record BackfillComplete(IImmutableList<ScoringStatsQueryResult> Results);

    protected override void PreStart()
    {
        var resolver = Context.GetActor<IRuleSetResolver>();
        resolver.Ask<RegisteredRuleSetsResult>(new QueryRegisteredRuleSets(), _backfillTimeout)
            .PipeTo(Self, failure: ex => new BackfillFailed(ex));
    }
}
