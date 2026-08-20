using Akka.Actor;
using Akka.Event;
using FunkArr.Configuration;
using Microsoft.Extensions.Options;

namespace FunkArr.RuleSet;

public sealed class MatchLedgerActor : ReceiveActor
{
    private readonly LinkedList<MatchRecord> _records = [];
    private readonly int _capacity;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public sealed record RecordMatchResult(MatchRecord Record);
    public sealed record GetRecentMatches(int Limit = 50);
    public sealed record RecentMatchesResponse(IReadOnlyList<MatchRecord> Records);
    public sealed record GetTopicStats(string Topic);
    public sealed record GetAllTopicStats;
    public sealed record TopicStatsResponse(IReadOnlyList<TopicStats> Stats);
    public sealed record GetUnmatchedItems(string? Topic = null);
    public sealed record UnmatchedItemsResponse(IReadOnlyList<UnmatchedGroup> Groups);

    public sealed record UnmatchedGroup(string Topic, IReadOnlyList<UnmatchedTrace> Items);

    public MatchLedgerActor(IOptions<FunkArrOptions> options)
    {
        _capacity = options.Value.MatchLedgerCapacity;

        Receive<RecordMatchResult>(HandleRecord);
        Receive<GetRecentMatches>(HandleGetRecent);
        Receive<GetTopicStats>(HandleGetTopicStats);
        Receive<GetAllTopicStats>(HandleGetAllTopicStats);
        Receive<GetUnmatchedItems>(HandleGetUnmatched);
    }

    private void HandleRecord(RecordMatchResult msg)
    {
        _records.AddFirst(msg.Record);

        while (_records.Count > _capacity)
        {
            _records.RemoveLast();
        }
    }

    private void HandleGetRecent(GetRecentMatches msg)
    {
        var result = _records
            .Take(msg.Limit)
            .ToList();

        Sender.Tell(new RecentMatchesResponse(result));
    }

    private void HandleGetTopicStats(GetTopicStats msg)
    {
        var stats = ComputeStatsForTopic(msg.Topic);
        Sender.Tell(new TopicStatsResponse(stats is not null ? [stats] : []));
    }

    private void HandleGetAllTopicStats(GetAllTopicStats _)
    {
        var topics = _records
            .Select(r => r.SearchTopic)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var stats = topics
            .Select(ComputeStatsForTopic)
            .Where(s => s is not null)
            .Cast<TopicStats>()
            .OrderBy(s => s.MatchRate)
            .ToList();

        Sender.Tell(new TopicStatsResponse(stats));
    }

    private void HandleGetUnmatched(GetUnmatchedItems msg)
    {
        var query = _records.AsEnumerable();
        if (msg.Topic is not null)
        {
            query = query.Where(r => r.SearchTopic.Equals(msg.Topic, StringComparison.OrdinalIgnoreCase));
        }

        var groups = query
            .Where(r => r.Unmatched.Count > 0)
            .SelectMany(r => r.Unmatched.Select(u => (r.SearchTopic, Trace: u)))
            .GroupBy(x => x.SearchTopic, StringComparer.OrdinalIgnoreCase)
            .Select(g => new UnmatchedGroup(g.Key, g.Select(x => x.Trace).ToList()))
            .OrderByDescending(g => g.Items.Count)
            .ToList();

        Sender.Tell(new UnmatchedItemsResponse(groups));
    }

    private TopicStats? ComputeStatsForTopic(string topic)
    {
        var topicRecords = _records
            .Where(r => r.SearchTopic.Equals(topic, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (topicRecords.Count == 0)
        {
            return null;
        }

        var totalItems = topicRecords.Sum(r => r.TotalResults);
        var matchedCount = topicRecords.Sum(r => r.Matched.Count);
        var filteredCount = topicRecords.Sum(r => r.Filtered.Count);
        var unmatchedCount = topicRecords.Sum(r => r.Unmatched.Count);
        var evaluated = matchedCount + filteredCount + unmatchedCount;

        var perRuleHits = new Dictionary<string, int>();
        foreach (var record in topicRecords)
        {
            foreach (var match in record.Matched)
            {
                var key = $"rule#{match.RuleIndex}:{match.Strategy}";
                perRuleHits.TryGetValue(key, out var count);
                perRuleHits[key] = count + 1;
            }
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
}
