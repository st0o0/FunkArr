using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Persistence;
using FunkArr.Core;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.MatchHistory;
using Microsoft.Extensions.Options;

namespace FunkArr.MatchMagic;

public sealed class MatchHistoryWorker : ReceivePersistentActor
{
    private readonly IOptionsMonitor<MatchHistoryOptions> _optionsMonitor;
    private readonly string _ruleSetId;
    private MatchHistoryState _state = MatchHistoryState.Empty;

    public override string PersistenceId => $"match-history-{_ruleSetId}";

    public MatchHistoryWorker(IOptionsMonitor<MatchHistoryOptions> optionsMonitor, string ruleSetId)
    {
        _ruleSetId = ruleSetId;
        _optionsMonitor = optionsMonitor;

        Context.SetReceiveTimeout(TimeSpan.FromMinutes(5));

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is MatchHistoryState snapshot)
            {
                _state = snapshot;
            }
        });

        Recover<ScoringRecorded>(evt =>
        {
            _state = _state.Apply(evt);
        });

        Command<RecordScoringResult>(cmd =>
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
        Command<ReceiveTimeout>(_ => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Command<SaveSnapshotSuccess>(_ => { });
        Command<SaveSnapshotFailure>(_ => { });
    }

    protected override void OnReplaySuccess()
    {
        base.OnReplaySuccess();
        var opts = _optionsMonitor.CurrentValue;
        _state = _state.Trim(opts.MaxSnapshots, opts.MaxAgeDays);
    }
}
