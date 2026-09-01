using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Persistence;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.Events.MatchHistory;

namespace FunkArr.MatchMagic;

public sealed class MatchHistoryWorker : ReceivePersistentActor
{
    private readonly int _maxSnapshots;
    private readonly int _maxAgeDays;
    private readonly int _snapshotInterval;
    private readonly string _ruleSetId;
    private MatchHistoryState _state = MatchHistoryState.Empty;

    public override string PersistenceId => $"match-history-{_ruleSetId}";

    public MatchHistoryWorker(string ruleSetId, int maxSnapshots = 100, int maxAgeDays = 30, int snapshotInterval = 20)
    {
        _ruleSetId = ruleSetId;
        _maxSnapshots = maxSnapshots;
        _maxAgeDays = maxAgeDays;
        _snapshotInterval = snapshotInterval;

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
                _state = newState.Trim(_maxSnapshots, _maxAgeDays);

                if (LastSequenceNr % _snapshotInterval == 0)
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
        _state = _state.Trim(_maxSnapshots, _maxAgeDays);
    }
}
