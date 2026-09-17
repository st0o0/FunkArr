using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Core;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.ScoringHistory;
using Microsoft.Extensions.Options;

namespace FunkArr.Scoring;

public sealed class ScoringHistoryWorker : ReceivePersistentActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IOptionsMonitor<ScoringHistoryOptions> _optionsMonitor;
    private ScoringHistoryState _state = ScoringHistoryState.Empty;

    public override string PersistenceId => $"scoring-history-{field}";

    public ScoringHistoryWorker(IOptionsMonitor<ScoringHistoryOptions> optionsMonitor, string ruleSetId)
    {
        PersistenceId = ruleSetId;
        _optionsMonitor = optionsMonitor;

        Context.SetReceiveTimeout(TimeSpan.FromMinutes(5));

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is ScoringHistoryState snapshot)
            {
                _state = snapshot;
            }
        });

        Recover<ScoringRecorded>(evt =>
        {
            _state = _state.Apply(evt);
        });

        Command<RecordScoring>(cmd =>
        {
            var (newState, evt) = _state.ProcessCommand(cmd);
            Persist(evt, _ =>
            {
                var opts = _optionsMonitor.CurrentValue;
                _state = newState.Trim(opts.MaxSnapshots, opts.MaxAgeDays);

                if (LastSequenceNr % opts.SnapshotInterval == 0)
                {
                    SaveSnapshot(_state);
                }
            });
        });

        Command<QueryScoringHistory>(query => Sender.Tell(_state.QueryHistory(query)));
        Command<QueryScoringDetail>(query => Sender.Tell(_state.QueryDetail(query)));
        Command<QueryScoringStats>(_ => Sender.Tell(_state.ToScoringStats()));
        Command<ReceiveTimeout>(_ => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Command<SaveSnapshotSuccess>(_ => { });
        Command<SaveSnapshotFailure>(f => _log.Warning(f.Cause, "Snapshot save failed at sequence {SequenceNr}", f.Metadata.SequenceNr));
    }

    protected override void OnReplaySuccess()
    {
        base.OnReplaySuccess();
        var opts = _optionsMonitor.CurrentValue;
        _state = _state.Trim(opts.MaxSnapshots, opts.MaxAgeDays);
    }
}
