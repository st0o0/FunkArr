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

        Recover<ScoringHistoryRecorded>(evt =>
        {
            _state = _state.Apply(evt);
        });

        Command<RecordHistory>(cmd =>
        {
            var (newState, evt) = _state.ProcessCommand(cmd);
            Persist(evt, _ =>
            {
                var opts = _optionsMonitor.CurrentValue;
                var beforeCount = newState.Snapshots.Count;
                _state = newState.Trim(opts.MaxSnapshots, opts.MaxAgeDays);
                var trimCount = beforeCount - _state.Snapshots.Count;

                if (trimCount > 0)
                {
                    Telemetry.Trimmed.Add(trimCount);
                }

                if (LastSequenceNr % opts.SnapshotInterval == 0)
                {
                    SaveSnapshot(_state.GetPersistenceState());
                }

                Telemetry.Recordings.Add(1);
                _statsCollector.Tell(new UpdateStats(ruleSetId, _state.Stats));
            });
        });

        Command<QueryScoringHistory>(query =>
        {
            Telemetry.Queries.Add(1, new KeyValuePair<string, object?>("type", "history"));
            Sender.Tell(_state.QueryHistory(query));
        });
        Command<QueryScoringDetail>(query =>
        {
            Telemetry.Queries.Add(1, new KeyValuePair<string, object?>("type", "detail"));
            Sender.Tell(_state.QueryDetail(query));
        });
        Command<QueryScoringStats>(_ =>
        {
            Telemetry.Queries.Add(1, new KeyValuePair<string, object?>("type", "stats"));
            Sender.Tell(_state.Stats);
        });
        Command<SaveSnapshotSuccess>(_ => { });
        Command<SaveSnapshotFailure>(f => _log.Warning(f.Cause, "Snapshot save failed at sequence {SequenceNr}", f.Metadata.SequenceNr));
    }

    protected override void OnReplaySuccess()
    {
        var opts = _optionsMonitor.CurrentValue;
        _state = _state.Trim(opts.MaxSnapshots, opts.MaxAgeDays);
    }
}
