using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.History;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using Microsoft.Extensions.Options;

namespace FunkArr.History.Tests;

public sealed class HistoryWorkerTests : TestKit
{
    private static readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private readonly TestProbe _statsCollectorProbe;
    private readonly ScoringHistoryOptions _options;

    public HistoryWorkerTests()
    {
        _statsCollectorProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IStatsCollector>(_statsCollectorProbe);
        _options = new ScoringHistoryOptions
        {
            MaxSnapshots = 100,
            MaxAgeDays = 30,
            SnapshotInterval = 20,
        };
    }

    private IActorRef CreateWorker(string ruleSetId = "rs-test")
    {
        var optionsMonitor = new TestOptionsMonitor(_options);
        return Sys.ActorOf(Props.Create(() => new HistoryWorker(optionsMonitor, ruleSetId)));
    }

    [Fact]
    public void RecordHistory_PersistsAndNotifiesStatsCollector()
    {
        var worker = CreateWorker();
        var requestId = Guid.NewGuid();
        var origin = new ScoringOrigin(SearchSource.Sonarr, "test-query");
        var cmd = new RecordHistory(requestId, "rs-test", origin, _now, 10, 5, 3, []);

        worker.Tell(cmd, TestActor);

        var update = _statsCollectorProbe.ExpectMsg<UpdateStats>();
        Assert.Equal("rs-test", update.RuleSetId);
        Assert.NotNull(update.Stats.LastRun);
        Assert.Equal(1, update.Stats.TotalRuns);
    }

    [Fact]
    public void RecordHistory_MultipleCommands_AccumulatesEntries()
    {
        var worker = CreateWorker();

        worker.Tell(CreateCommand(Guid.NewGuid()), TestActor);
        _statsCollectorProbe.ExpectMsg<UpdateStats>();

        worker.Tell(CreateCommand(Guid.NewGuid()), TestActor);
        _statsCollectorProbe.ExpectMsg<UpdateStats>();

        worker.Tell(CreateCommand(Guid.NewGuid()), TestActor);
        var update = _statsCollectorProbe.ExpectMsg<UpdateStats>();
        Assert.Equal(3, update.Stats.TotalRuns);
    }

    [Fact]
    public void QueryScoringHistory_ReturnsPaginatedResults()
    {
        var worker = CreateWorker();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        worker.Tell(CreateCommand(id1, timestamp: _now), TestActor);
        _statsCollectorProbe.ExpectMsg<UpdateStats>();

        worker.Tell(CreateCommand(id2, timestamp: _now.AddHours(1)), TestActor);
        _statsCollectorProbe.ExpectMsg<UpdateStats>();

        worker.Tell(CreateCommand(id3, timestamp: _now.AddHours(2)), TestActor);
        _statsCollectorProbe.ExpectMsg<UpdateStats>();

        worker.Tell(new QueryScoringHistory("rs-test", 0, 2), TestActor);

        var result = ExpectMsg<ScoringHistoryResult>();
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Snapshots.Length);
        Assert.Equal(id3, result.Snapshots[0].RequestId);
        Assert.Equal(id2, result.Snapshots[1].RequestId);
    }

    [Fact]
    public void QueryScoringDetail_KnownRequestId_ReturnsResult()
    {
        var worker = CreateWorker();
        var requestId = Guid.NewGuid();

        worker.Tell(CreateCommand(requestId, candidateCount: 8, matchedCount: 4), TestActor);
        _statsCollectorProbe.ExpectMsg<UpdateStats>();

        worker.Tell(new QueryScoringDetail("rs-test", requestId), TestActor);

        var response = ExpectMsg<ScoringDetailResponse>();
        var result = Assert.IsType<ScoringDetailResult>(response);
        Assert.Equal(requestId, result.RequestId);
        Assert.Equal(SearchSource.Sonarr, result.Source);
        Assert.Equal("test-query", result.Query);
    }

    [Fact]
    public void QueryScoringDetail_UnknownRequestId_ReturnsFailed()
    {
        var worker = CreateWorker();

        worker.Tell(new QueryScoringDetail("rs-test", Guid.NewGuid()), TestActor);

        var response = ExpectMsg<ScoringDetailResponse>();
        Assert.IsType<ScoringDetailFailed>(response);
    }

    [Fact]
    public void QueryScoringStats_ReturnsCurrentStats()
    {
        var worker = CreateWorker();

        worker.Tell(CreateCommand(Guid.NewGuid(), candidateCount: 10, matchedCount: 5, enrichedCount: 3), TestActor);
        _statsCollectorProbe.ExpectMsg<UpdateStats>();

        worker.Tell(new QueryScoringStats("rs-test"), TestActor);

        var result = ExpectMsg<ScoringStatsQueryResult>();
        Assert.Equal("rs-test", result.RuleSetId);
        Assert.NotNull(result.Stats.LastRun);
        Assert.NotNull(result.Stats.MatchRate);
        Assert.Equal(0.5, result.Stats.MatchRate.Value, 0.001);
        Assert.NotNull(result.Stats.EnrichmentRate);
        Assert.Equal(0.6, result.Stats.EnrichmentRate.Value, 0.001);
        Assert.Equal(1, result.Stats.TotalRuns);
    }

    [Fact]
    public void QueryScoringStats_EmptyState_ReturnsNullRates()
    {
        var worker = CreateWorker();

        worker.Tell(new QueryScoringStats("rs-test"), TestActor);

        var result = ExpectMsg<ScoringStatsQueryResult>();
        Assert.Equal("rs-test", result.RuleSetId);
        Assert.Null(result.Stats.LastRun);
        Assert.Null(result.Stats.MatchRate);
        Assert.Equal(0, result.Stats.TotalRuns);
    }

    [Fact]
    public void RecordHistory_WithItemTraces_PreservesTraceData()
    {
        var worker = CreateWorker();
        var requestId = Guid.NewGuid();
        var candidate = new ScoreCandidate("Tatort", "Krimi", "ARD", 5400, 720, "A crime story", 1719360000);
        var traces = new[]
        {
            new ItemTrace(candidate, true, 0.95, "rule-1",
                new TracedIdentification("1", "5", "Tatort"),
                [new RuleTrace("rule-1", 0, RuleOutcome.Matched, null, null)]),
        };

        var cmd = new RecordHistory(requestId, "rs-test",
            new ScoringOrigin(SearchSource.Sonarr, "tatort"), _now, 1, 1, 0, traces);

        worker.Tell(cmd, TestActor);
        _statsCollectorProbe.ExpectMsg<UpdateStats>();

        worker.Tell(new QueryScoringDetail("rs-test", requestId), TestActor);

        var response = ExpectMsg<ScoringDetailResponse>();
        var result = Assert.IsType<ScoringDetailResult>(response);
        var trace = Assert.Single(result.ItemTraces);
        Assert.True(trace.Matched);
        Assert.Equal(0.95, trace.Score, 0.001);
        Assert.Equal("rule-1", trace.MatchedRuleId);
        Assert.NotNull(trace.Identification);
        Assert.Equal("1", trace.Identification.Season);
        Assert.Equal("5", trace.Identification.Episode);
        Assert.Equal("Tatort", trace.Candidate.Title);
    }

    [Fact]
    public void PersistenceId_IncludesRuleSetId()
    {
        var worker = CreateWorker("my-ruleset");

        worker.Tell(new QueryScoringStats("my-ruleset"), TestActor);

        var result = ExpectMsg<ScoringStatsQueryResult>();
        Assert.Equal("my-ruleset", result.RuleSetId);
    }

    private static RecordHistory CreateCommand(
        Guid requestId,
        int candidateCount = 5,
        int matchedCount = 2,
        int enrichedCount = 1,
        DateTimeOffset? timestamp = null) =>
        new(requestId, "rs-test", new ScoringOrigin(SearchSource.Sonarr, "test-query"),
            timestamp ?? _now, candidateCount, matchedCount, enrichedCount, []);

    private sealed class TestOptionsMonitor(ScoringHistoryOptions options) : IOptionsMonitor<ScoringHistoryOptions>
    {
        public ScoringHistoryOptions CurrentValue => options;
        public ScoringHistoryOptions Get(string? name) => options;
        public IDisposable? OnChange(Action<ScoringHistoryOptions, string?> listener) => null;
    }
}
