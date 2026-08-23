using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Persistence;

namespace FunkArr.RuleSet;

public sealed class MatchQualityWorker : ReceivePersistentActor, IWithTimers
{
    public override string PersistenceId => "match-quality";

    private const int SnapshotInterval = 500;
    private static readonly TimeSpan EvictionInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7);

    private readonly LinkedList<MatchRecord> _records = [];
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private long _eventCount;

    public ITimerScheduler Timers { get; set; } = null!;

    public sealed record RecordMatchResult(MatchRecord Record);
    public sealed record GetRecentMatches(int Limit = 50);
    public sealed record RecentMatchesResponse(IReadOnlyList<MatchRecord> Records);
    public sealed record GetTopicStats(string Topic);
    public sealed record GetAllTopicStats;
    public sealed record TopicStatsResponse(IReadOnlyList<TopicStats> Stats);
    public sealed record GetUnmatchedItems(string? Topic = null);
    public sealed record UnmatchedItemsResponse(IReadOnlyList<UnmatchedGroup> Groups);
    public sealed record UnmatchedGroup(string Topic, IReadOnlyList<UnmatchedTrace> Items);

    public sealed record MatchRecorded(MatchRecord Record);
    public sealed record MatchesExpired(DateTimeOffset OlderThan);

    private sealed record EvictionTick;

    public MatchQualityWorker()
    {
        Recovering();
    }

    private void Recovering()
    {
        Recover<MatchRecordedDto>(dto => Apply(MatchQualityEventDtoMapping.ToDomain(dto)));
        Recover<MatchesExpiredDto>(dto => Apply(MatchQualityEventDtoMapping.ToDomain(dto)));
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is MatchQualitySnapshot snapshot)
                ApplySnapshot(snapshot);
        });
        Recover<RecoveryCompleted>(_ =>
        {
            _log.Info("MatchQualityWorker recovery completed. {Count} records", _records.Count);
            Become(Ready);
            Timers.StartPeriodicTimer("eviction", new EvictionTick(), EvictionInterval, EvictionInterval);
        });
    }

    private void Ready()
    {
        Command<RecordMatchResult>(HandleRecord);
        Command<GetRecentMatches>(HandleGetRecent);
        Command<GetTopicStats>(HandleGetTopicStats);
        Command<GetAllTopicStats>(HandleGetAllTopicStats);
        Command<GetUnmatchedItems>(HandleGetUnmatched);
        Command<EvictionTick>(_ => RunEviction());
        Command<SaveSnapshotSuccess>(msg => DeleteMessages(msg.Metadata.SequenceNr));
        Command<SaveSnapshotFailure>(msg => _log.Warning("Snapshot failed: {Cause}", msg.Cause.Message));
    }

    private void HandleRecord(RecordMatchResult msg)
    {
        var evt = new MatchRecorded(msg.Record);
        Persist(MatchQualityEventDtoMapping.ToDto(evt), _ =>
        {
            Apply(evt);
            IncrementAndSnapshot();
        });
    }

    private void RunEviction()
    {
        var threshold = DateTimeOffset.UtcNow - RetentionPeriod;
        if (_records.Count == 0 || _records.Last!.Value.Timestamp >= threshold) return;

        var evt = new MatchesExpired(threshold);
        Persist(MatchQualityEventDtoMapping.ToDto(evt), _ =>
        {
            Apply(evt);
            IncrementAndSnapshot();
            _log.Debug("Evicted records older than {Threshold}, {Count} remaining", threshold, _records.Count);
        });
    }

    private void HandleGetRecent(GetRecentMatches msg)
    {
        Sender.Tell(new RecentMatchesResponse(_records.Take(msg.Limit).ToList()));
    }

    private void HandleGetTopicStats(GetTopicStats msg)
    {
        var stats = ComputeStatsForTopic(msg.Topic);
        Sender.Tell(new TopicStatsResponse(stats is not null ? [stats] : []));
    }

    private void HandleGetAllTopicStats(GetAllTopicStats _)
    {
        var topics = _records.Select(r => r.SearchTopic).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var stats = topics.Select(ComputeStatsForTopic).Where(s => s is not null).Cast<TopicStats>()
            .OrderBy(s => s.MatchRate).ToList();
        Sender.Tell(new TopicStatsResponse(stats));
    }

    private void HandleGetUnmatched(GetUnmatchedItems msg)
    {
        var query = _records.AsEnumerable();
        if (msg.Topic is not null)
            query = query.Where(r => r.SearchTopic.Equals(msg.Topic, StringComparison.OrdinalIgnoreCase));

        var groups = query.Where(r => r.Unmatched.Count > 0)
            .SelectMany(r => r.Unmatched.Select(u => (r.SearchTopic, Trace: u)))
            .GroupBy(x => x.SearchTopic, StringComparer.OrdinalIgnoreCase)
            .Select(g => new UnmatchedGroup(g.Key, g.Select(x => x.Trace).ToList()))
            .OrderByDescending(g => g.Items.Count).ToList();

        Sender.Tell(new UnmatchedItemsResponse(groups));
    }

    private TopicStats? ComputeStatsForTopic(string topic)
    {
        var topicRecords = _records.Where(r => r.SearchTopic.Equals(topic, StringComparison.OrdinalIgnoreCase)).ToList();
        if (topicRecords.Count == 0) return null;

        var matchedCount = topicRecords.Sum(r => r.Matched.Count);
        var filteredCount = topicRecords.Sum(r => r.Filtered.Count);
        var unmatchedCount = topicRecords.Sum(r => r.Unmatched.Count);
        var evaluated = matchedCount + filteredCount + unmatchedCount;

        var perRuleHits = new Dictionary<string, int>();
        foreach (var record in topicRecords)
            foreach (var match in record.Matched)
            {
                var key = $"rule#{match.RuleIndex}:{match.Strategy}";
                perRuleHits.TryGetValue(key, out var count);
                perRuleHits[key] = count + 1;
            }

        return new TopicStats
        {
            Topic = topic,
            SearchCount = topicRecords.Count,
            TotalItemsEvaluated = evaluated,
            MatchedCount = matchedCount,
            FilteredCount = filteredCount,
            UnmatchedCount = unmatchedCount,
            MatchRate = evaluated > 0 ? (double)matchedCount / evaluated : 0.0,
            PerRuleHitCounts = perRuleHits,
        };
    }

    private void Apply(MatchRecorded evt)
    {
        _records.AddFirst(evt.Record);
    }

    private void Apply(MatchesExpired evt)
    {
        while (_records.Count > 0 && _records.Last!.Value.Timestamp < evt.OlderThan)
            _records.RemoveLast();
    }

    private void ApplySnapshot(MatchQualitySnapshot snapshot)
    {
        _records.Clear();
        foreach (var record in snapshot.Records)
            _records.AddLast(record);
    }

    private void IncrementAndSnapshot()
    {
        _eventCount++;
        if (_eventCount % SnapshotInterval == 0)
            SaveSnapshot(new MatchQualitySnapshot(_records.ToList()));
    }
}

internal sealed record MatchQualitySnapshot(List<MatchRecord> Records);
