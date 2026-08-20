using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;
using FunkArr.Configuration;
using FunkArr.RuleSet;
using FunkArr.Search;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.RuleSet;

public class MatchLedgerActorTests : Akka.Hosting.TestKit.TestKit
{
    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton(Options.Create(new FunkArrOptions { MatchLedgerCapacity = 10000 }));
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    private IActorRef CreateActor(int capacity = 5)
    {
        var props = Props.Create(() =>
            new MatchLedgerActor(Options.Create(new FunkArrOptions { MatchLedgerCapacity = capacity })));
        return Sys.ActorOf(props);
    }

    [Fact(Timeout = 5000)]
    public async Task RecordAndRetrieve_ReturnsRecordedMatch()
    {
        var actor = CreateActor();
        var record = CreateRecord("topic-a", matched: 1);

        actor.Tell(new MatchLedgerActor.RecordMatchResult(record));
        var response = await actor.Ask<MatchLedgerActor.RecentMatchesResponse>(
            new MatchLedgerActor.GetRecentMatches(), TimeSpan.FromSeconds(3));

        Assert.Single(response.Records);
        Assert.Equal("topic-a", response.Records[0].SearchTopic);
    }

    [Fact(Timeout = 5000)]
    public async Task GetRecentMatches_RespectsLimit()
    {
        var actor = CreateActor(capacity: 10);

        for (var i = 0; i < 5; i++)
            actor.Tell(new MatchLedgerActor.RecordMatchResult(CreateRecord("t", id: $"id-{i}")));

        var response = await actor.Ask<MatchLedgerActor.RecentMatchesResponse>(
            new MatchLedgerActor.GetRecentMatches(Limit: 2), TimeSpan.FromSeconds(3));

        Assert.Equal(2, response.Records.Count);
    }

    [Fact(Timeout = 5000)]
    public async Task CapacityEviction_OldestRecordsAreRemoved()
    {
        var actor = CreateActor(capacity: 3);

        for (var i = 0; i < 5; i++)
            actor.Tell(new MatchLedgerActor.RecordMatchResult(CreateRecord("t", id: $"id-{i}")));

        var response = await actor.Ask<MatchLedgerActor.RecentMatchesResponse>(
            new MatchLedgerActor.GetRecentMatches(Limit: 50), TimeSpan.FromSeconds(3));

        Assert.Equal(3, response.Records.Count);
        Assert.Equal("id-4", response.Records[0].Id);
        Assert.Equal("id-3", response.Records[1].Id);
        Assert.Equal("id-2", response.Records[2].Id);
    }

    [Fact(Timeout = 5000)]
    public async Task TopicStats_ComputesCorrectCounts()
    {
        var actor = CreateActor(capacity: 100);

        actor.Tell(new MatchLedgerActor.RecordMatchResult(
            CreateRecord("crime", matched: 3, filtered: 1, unmatched: 2, totalResults: 10)));
        actor.Tell(new MatchLedgerActor.RecordMatchResult(
            CreateRecord("crime", matched: 2, filtered: 0, unmatched: 1, totalResults: 8)));

        var response = await actor.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetTopicStats("crime"), TimeSpan.FromSeconds(3));

        Assert.Single(response.Stats);
        var stats = response.Stats[0];
        Assert.Equal("crime", stats.Topic);
        Assert.Equal(2, stats.SearchCount);
        Assert.Equal(5, stats.MatchedCount);
        Assert.Equal(1, stats.FilteredCount);
        Assert.Equal(3, stats.UnmatchedCount);
        Assert.Equal(9, stats.TotalItemsEvaluated);
        Assert.Equal(5.0 / 9, stats.MatchRate, precision: 10);
    }

    [Fact(Timeout = 5000)]
    public async Task TopicStats_UnknownTopic_ReturnsEmpty()
    {
        var actor = CreateActor();

        var response = await actor.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetTopicStats("nonexistent"), TimeSpan.FromSeconds(3));

        Assert.Empty(response.Stats);
    }

    [Fact(Timeout = 5000)]
    public async Task TopicStats_CaseInsensitiveLookup()
    {
        var actor = CreateActor();

        actor.Tell(new MatchLedgerActor.RecordMatchResult(CreateRecord("Tatort", matched: 2)));

        var response = await actor.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetTopicStats("tatort"), TimeSpan.FromSeconds(3));

        Assert.Single(response.Stats);
        Assert.Equal(2, response.Stats[0].MatchedCount);
    }

    [Fact(Timeout = 5000)]
    public async Task AllTopicStats_SortedByMatchRateAscending()
    {
        var actor = CreateActor(capacity: 100);

        actor.Tell(new MatchLedgerActor.RecordMatchResult(
            CreateRecord("bad-topic", matched: 1, unmatched: 4)));
        actor.Tell(new MatchLedgerActor.RecordMatchResult(
            CreateRecord("good-topic", matched: 4, unmatched: 1)));
        actor.Tell(new MatchLedgerActor.RecordMatchResult(
            CreateRecord("mid-topic", matched: 1, unmatched: 1)));

        var response = await actor.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetAllTopicStats(), TimeSpan.FromSeconds(3));

        Assert.Equal(3, response.Stats.Count);
        Assert.Equal("bad-topic", response.Stats[0].Topic);
        Assert.Equal("mid-topic", response.Stats[1].Topic);
        Assert.Equal("good-topic", response.Stats[2].Topic);
    }

    [Fact(Timeout = 5000)]
    public async Task UnmatchedItems_GroupedByTopic_SortedByCountDescending()
    {
        var actor = CreateActor(capacity: 100);

        actor.Tell(new MatchLedgerActor.RecordMatchResult(CreateRecord("few", unmatched: 1)));
        actor.Tell(new MatchLedgerActor.RecordMatchResult(CreateRecord("many", unmatched: 3)));

        var response = await actor.Ask<MatchLedgerActor.UnmatchedItemsResponse>(
            new MatchLedgerActor.GetUnmatchedItems(), TimeSpan.FromSeconds(3));

        Assert.Equal(2, response.Groups.Count);
        Assert.Equal("many", response.Groups[0].Topic);
        Assert.Equal(3, response.Groups[0].Items.Count);
        Assert.Equal("few", response.Groups[1].Topic);
        Assert.Single(response.Groups[1].Items);
    }

    [Fact(Timeout = 5000)]
    public async Task UnmatchedItems_FilteredByTopic()
    {
        var actor = CreateActor(capacity: 100);

        actor.Tell(new MatchLedgerActor.RecordMatchResult(CreateRecord("alpha", unmatched: 2)));
        actor.Tell(new MatchLedgerActor.RecordMatchResult(CreateRecord("beta", unmatched: 3)));

        var response = await actor.Ask<MatchLedgerActor.UnmatchedItemsResponse>(
            new MatchLedgerActor.GetUnmatchedItems("alpha"), TimeSpan.FromSeconds(3));

        Assert.Single(response.Groups);
        Assert.Equal("alpha", response.Groups[0].Topic);
        Assert.Equal(2, response.Groups[0].Items.Count);
    }

    [Fact(Timeout = 5000)]
    public async Task UnmatchedItems_NoUnmatched_ReturnsEmpty()
    {
        var actor = CreateActor();

        actor.Tell(new MatchLedgerActor.RecordMatchResult(
            CreateRecord("clean", matched: 5, unmatched: 0)));

        var response = await actor.Ask<MatchLedgerActor.UnmatchedItemsResponse>(
            new MatchLedgerActor.GetUnmatchedItems(), TimeSpan.FromSeconds(3));

        Assert.Empty(response.Groups);
    }

    [Fact(Timeout = 5000)]
    public async Task EmptyLedger_RecentMatches_ReturnsEmpty()
    {
        var actor = CreateActor();

        var response = await actor.Ask<MatchLedgerActor.RecentMatchesResponse>(
            new MatchLedgerActor.GetRecentMatches(), TimeSpan.FromSeconds(3));

        Assert.Empty(response.Records);
    }

    [Fact(Timeout = 5000)]
    public async Task EmptyLedger_AllTopicStats_ReturnsEmpty()
    {
        var actor = CreateActor();

        var response = await actor.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetAllTopicStats(), TimeSpan.FromSeconds(3));

        Assert.Empty(response.Stats);
    }

    [Fact(Timeout = 5000)]
    public async Task EmptyLedger_UnmatchedItems_ReturnsEmpty()
    {
        var actor = CreateActor();

        var response = await actor.Ask<MatchLedgerActor.UnmatchedItemsResponse>(
            new MatchLedgerActor.GetUnmatchedItems(), TimeSpan.FromSeconds(3));

        Assert.Empty(response.Groups);
    }

    [Fact(Timeout = 5000)]
    public async Task TopicStats_PerRuleHitCounts_AggregatedAcrossRecords()
    {
        var actor = CreateActor(capacity: 100);

        var matched1 = new MatchedTrace
        {
            ItemTitle = "ep1", ItemTopic = "show", ItemDuration = 2700, ItemChannel = "ARD",
            RuleIndex = 0, Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
            Confidence = 1.0, Season = 1, Episode = 1, EpisodeName = "Ep1",
        };
        var matched2 = new MatchedTrace
        {
            ItemTitle = "ep2", ItemTopic = "show", ItemDuration = 2700, ItemChannel = "ARD",
            RuleIndex = 1, Strategy = MatchingStrategy.ItemTitleExact,
            Confidence = 0.9, Season = 1, Episode = 2, EpisodeName = "Ep2",
        };

        var record1 = new MatchRecord
        {
            Id = "r1", Timestamp = DateTimeOffset.UtcNow, SearchTopic = "show",
            TvdbId = 1, Season = 1, Episode = null, Source = "test", TotalResults = 5,
            Matched = [matched1], Filtered = [], Unmatched = [],
        };
        var record2 = new MatchRecord
        {
            Id = "r2", Timestamp = DateTimeOffset.UtcNow, SearchTopic = "show",
            TvdbId = 1, Season = 1, Episode = null, Source = "test", TotalResults = 5,
            Matched = [matched1, matched2], Filtered = [], Unmatched = [],
        };

        actor.Tell(new MatchLedgerActor.RecordMatchResult(record1));
        actor.Tell(new MatchLedgerActor.RecordMatchResult(record2));

        var response = await actor.Ask<MatchLedgerActor.TopicStatsResponse>(
            new MatchLedgerActor.GetTopicStats("show"), TimeSpan.FromSeconds(3));

        var stats = response.Stats[0];
        Assert.Equal(2, stats.PerRuleHitCounts["rule#0:SeasonAndEpisodeNumber"]);
        Assert.Equal(1, stats.PerRuleHitCounts["rule#1:ItemTitleExact"]);
    }

    private static MatchRecord CreateRecord(
        string topic,
        int matched = 0,
        int filtered = 0,
        int unmatched = 0,
        int totalResults = 0,
        string? id = null)
    {
        return new MatchRecord
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            SearchTopic = topic,
            TvdbId = null,
            Season = null,
            Episode = null,
            Source = "test",
            TotalResults = totalResults > 0 ? totalResults : matched + filtered + unmatched,
            Matched = Enumerable.Range(0, matched).Select(i => new MatchedTrace
            {
                ItemTitle = $"matched-{i}", ItemTopic = topic, ItemDuration = 2700, ItemChannel = "ARD",
                RuleIndex = 0, Strategy = MatchingStrategy.SeasonAndEpisodeNumber,
                Confidence = 1.0, Season = 1, Episode = i + 1, EpisodeName = $"Ep{i + 1}",
            }).ToList(),
            Filtered = Enumerable.Range(0, filtered).Select(i => new FilteredTrace
            {
                ItemTitle = $"filtered-{i}", ItemTopic = topic, ItemDuration = 300, ItemChannel = "ARD",
                FilterField = "duration", FilterOp = ">", FilterValue = "35", ActualValue = "5",
                Reason = "Duration too short",
            }).ToList(),
            Unmatched = Enumerable.Range(0, unmatched).Select(i => new UnmatchedTrace
            {
                ItemTitle = $"unmatched-{i}", ItemTopic = topic, ItemDuration = 2700, ItemChannel = "ZDF",
                RuleFailures = [new RuleFailure { RuleIndex = 0, FailReason = "No regex match" }],
            }).ToList(),
        };
    }
}
