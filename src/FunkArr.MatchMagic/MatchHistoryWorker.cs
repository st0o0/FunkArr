using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Persistence;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence.MatchHistory;

namespace FunkArr.MatchMagic;

public sealed class MatchHistoryWorker : ReceivePersistentActor
{
    private sealed record State(ImmutableList<ScoringSnapshot> Snapshots);

    private sealed record ScoringSnapshot(
        Guid RequestId,
        ScoringOrigin Origin,
        DateTimeOffset Timestamp,
        int CandidateCount,
        int MatchedCount,
        ItemTrace[] ItemTraces);

    private readonly int _maxSnapshots;
    private readonly int _maxAgeDays;
    private readonly int _snapshotInterval;
    private readonly string _ruleSetId;
    private State _state = new(ImmutableList<ScoringSnapshot>.Empty);
    private int _eventsSinceSnapshot;

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
            if (offer.Snapshot is MatchHistorySnapshotDto snapshot)
            {
                _state = RestoreFromSnapshot(snapshot);
            }
        });

        Recover<ScoringRecordedDto>(dto =>
        {
            _state = ApplyEvent(dto);
        });

        Command<RecordScoringResult>(HandleRecord);
        Command<QueryScoringHistory>(HandleQueryHistory);
        Command<QueryScoringDetail>(HandleQueryDetail);
        Command<ReceiveTimeout>(_ => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Command<SaveSnapshotSuccess>(_ => { });
        Command<SaveSnapshotFailure>(_ => { });
    }

    protected override void OnReplaySuccess()
    {
        base.OnReplaySuccess();
        _state = Trim(_state);
    }

    private void HandleRecord(RecordScoringResult msg)
    {
        var dto = ScoringRecordedMapper.ToDto(msg);
        Persist(dto, persisted =>
        {
            _state = ApplyEvent(persisted);
            _state = Trim(_state);
            _eventsSinceSnapshot++;

            if (_eventsSinceSnapshot >= _snapshotInterval)
            {
                SaveSnapshot(CreateSnapshot());
                _eventsSinceSnapshot = 0;
            }
        });
    }

    private void HandleQueryHistory(QueryScoringHistory query)
    {
        var snapshots = _state.Snapshots;
        var total = snapshots.Count;
        var page = snapshots
            .Reverse()
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(s => new ScoringSnapshotSummary(
                s.RequestId,
                s.Origin.Source,
                s.Origin.Query,
                s.Timestamp,
                s.CandidateCount,
                s.MatchedCount))
            .ToArray();

        Sender.Tell(new ScoringHistoryResult(query.RuleSetId, total, page));
    }

    private void HandleQueryDetail(QueryScoringDetail query)
    {
        var snapshot = _state.Snapshots.FirstOrDefault(s => s.RequestId == query.RequestId);
        if (snapshot is null)
        {
            Sender.Tell(new ScoringDetailNotFound(query.RequestId));
            return;
        }

        Sender.Tell(new ScoringDetailResult(
            snapshot.RequestId,
            snapshot.Origin.Source,
            snapshot.Origin.Query,
            snapshot.Timestamp,
            snapshot.ItemTraces));
    }

    private State ApplyEvent(ScoringRecordedDto dto)
    {
        var result = ScoringRecordedMapper.FromDto(dto, _ruleSetId);
        var snapshot = new ScoringSnapshot(
            result.RequestId,
            result.Origin,
            result.Timestamp,
            result.CandidateCount,
            result.MatchedCount,
            result.ItemTraces);
        return _state with { Snapshots = _state.Snapshots.Add(snapshot) };
    }

    private State Trim(State state)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-_maxAgeDays);
        var trimmed = state.Snapshots
            .Where(s => s.Timestamp >= cutoff)
            .ToImmutableList();

        if (trimmed.Count > _maxSnapshots)
        {
            trimmed = trimmed.Skip(trimmed.Count - _maxSnapshots).ToImmutableList();
        }

        return state with { Snapshots = trimmed };
    }

    private MatchHistorySnapshotDto CreateSnapshot()
    {
        return new MatchHistorySnapshotDto
        {
            Snapshots = _state.Snapshots.Select(s => new MatchHistorySnapshotDto.SnapshotEntryDto
            {
                RequestId = s.RequestId.ToString(),
                Source = s.Origin.Source,
                Query = s.Origin.Query,
                Timestamp = s.Timestamp.ToString("O"),
                CandidateCount = s.CandidateCount,
                MatchedCount = s.MatchedCount,
                ItemTraces = s.ItemTraces.Select(ScoringRecordedMapper.ItemTraceToDto).ToArray()
            }).ToArray()
        };
    }

    private State RestoreFromSnapshot(MatchHistorySnapshotDto snapshot)
    {
        var entries = snapshot.Snapshots.Select(e => new ScoringSnapshot(
            Guid.Parse(e.RequestId),
            new ScoringOrigin(e.Source, e.Query),
            DateTimeOffset.Parse(e.Timestamp),
            e.CandidateCount,
            e.MatchedCount,
            e.ItemTraces.Select(ScoringRecordedMapper.ItemTraceFromDto).ToArray()
        )).ToImmutableList();

        return new State(entries);
    }
}
