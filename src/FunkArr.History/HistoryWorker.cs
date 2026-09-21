using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Core;
using FunkArr.Messages.History;
using FunkArr.Persistence.Events.ScoringHistory;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.History;

public sealed class HistoryWorker : ReceivePersistentActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IOptionsMonitor<ScoringHistoryOptions> _optionsMonitor;
    private readonly IActorRef _statsCollector = Context.GetActor<IStatsCollector>();
    private HistoryState _state = HistoryState.Empty;

    public override string PersistenceId { get; }

    public HistoryWorker(IOptionsMonitor<ScoringHistoryOptions> optionsMonitor, string ruleSetId)
    {
        PersistenceId = $"history-{ruleSetId}";
        _optionsMonitor = optionsMonitor;

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is PersistedHistoryState persisted)
            {
                _state = HistoryState.FromPersistence(persisted);
            }
        });

        Recover<HistoryRecorded>(evt =>
        {
            _state = _state.Apply(evt);
        });

        Command<RecordHistory>(cmd =>
        {
            var (newState, evt) = _state.ProcessCommand(cmd);
            Persist(evt, _ =>
            {
                var opts = _optionsMonitor.CurrentValue;
                _state = newState.Trim(opts.MaxSnapshots, opts.MaxAgeDays);

                if (LastSequenceNr % opts.SnapshotInterval == 0)
                {
                    SaveSnapshot(_state.GetPersistenceState());
                }

                _statsCollector.Tell(new StatsUpdated(ruleSetId, _state.Stats));
            });
        });

        Command<QueryScoringHistory>(query => Sender.Tell(_state.QueryHistory(query)));
        Command<QueryScoringDetail>(query => Sender.Tell(_state.QueryDetail(query)));
        Command<QueryScoringStats>(_ => Sender.Tell(_state.Stats));
        Command<SaveSnapshotSuccess>(_ => { });
        Command<SaveSnapshotFailure>(f => _log.Warning(f.Cause, "Snapshot save failed at sequence {SequenceNr}", f.Metadata.SequenceNr));
    }

    protected override void OnReplaySuccess()
    {
        var opts = _optionsMonitor.CurrentValue;
        _state = _state.Trim(opts.MaxSnapshots, opts.MaxAgeDays);
    }
}
