using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using Microsoft.Extensions.Options;

namespace FunkArr.Feed.Tests;

public sealed class TvEvaluationWorkerTests : TestKit
{
    private sealed record TestProbes(
        Akka.TestKit.TestProbe Scoring,
        Akka.TestKit.TestProbe Resolver,
        Akka.TestKit.TestProbe Enrichment,
        Akka.TestKit.TestProbe History);

    private TestProbes RegisterProbes()
    {
        var probes = new TestProbes(CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe());
        var registry = ActorRegistry.For(Sys);
        registry.Register<IScoringManager>(probes.Scoring);
        registry.Register<IRuleSetResolver>(probes.Resolver);
        registry.Register<IEnrichmentManager>(probes.Enrichment);
        registry.Register<IHistoryRegion>(probes.History);
        return probes;
    }

    private IActorRef CreateWorker(Guid evaluationId) =>
        Sys.ActorOf(Props.Create(() => new TvEvaluationWorker(evaluationId.ToString(), (IOptionsMonitor<VariantOptions>?)null)));

    [Fact]
    public void Evaluation_resolves_ruleset_from_topic_and_returns_scored_results()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateSeries(evaluationId, SearchSource.Sonarr,
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Test", null, 1719244800, 5400, 1200000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null)
        ]), TestActor);

        var resolveRequest = p.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Equal("Tatort", resolveRequest.TopicOrAlias);
        p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

        var scoreRequest = p.Scoring.ExpectMsg<ScoreItems>();
        Assert.Multiple(
            () => Assert.Single(scoreRequest.Candidates),
            () => Assert.Equal("tatort", scoreRequest.RuleSetId),
            () => Assert.Equal(SearchSource.Sonarr, scoreRequest.Origin.Source));
        p.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.95, true)], []));

        var result = ExpectMsg<EvaluateSeriesCompleted>();
        Assert.Equal(evaluationId, result.SearchId);
        Assert.Equal(2, result.Items.Length);
        Assert.Contains(result.Items, i => i.Title.Contains("1080p") && i.Url == "https://example.com/hd.mp4");
        Assert.Contains(result.Items, i => i.Title.Contains("720p") && i.Url == "https://example.com/sd.mp4");
        Assert.All(result.Items, i => Assert.Equal(0.95, i.Score));
    }

    [Fact]
    public void Evaluation_with_resolved_ruleset_skips_resolution()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateSeries(evaluationId, SearchSource.Sonarr, [Item("Tatort", "Tatort: Test")],
            TvdbId: 83214, RuleSet: new RuleSetResolved("tatort", "Tatort")), TestActor);

        var scoreRequest = p.Scoring.ExpectMsg<ScoreItems>();
        Assert.Equal("tatort", scoreRequest.RuleSetId);
        p.Resolver.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
        p.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.9, true)], []));

        var result = ExpectMsg<EvaluateSeriesCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Equal(83214, item.Metadata?.Ids?.TvdbId);
    }

    [Fact]
    public void Scoring_failure_returns_evaluation_failed()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateSeries(evaluationId, SearchSource.Sonarr, [Item("Tatort", "Test")]), TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));
        p.Scoring.ExpectMsg<ScoreItems>();
        p.Scoring.Reply(new ScoringFailed(new Exception("Scoring error")));

        var result = ExpectMsg<EvaluateSeriesFailed>();
        Assert.Multiple(
            () => Assert.Equal(evaluationId, result.SearchId),
            () => Assert.Contains("Scoring error", result.Cause.Message));
    }

    [Fact]
    public void RuleSet_not_found_returns_unscored_results()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateSeries(evaluationId, SearchSource.Sonarr, [Item("Unknown Show", "Episode 1", "ZDF")]),
            TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetFailed(new RuleSetNotFoundException("Unknown Show")));

        var result = ExpectMsg<EvaluateSeriesCompleted>();
        Assert.Equal(evaluationId, result.SearchId);
        var item = Assert.Single(result.Items);
        Assert.Equal(0.0, item.Score);
        p.Scoring.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void Evaluation_with_no_items_replies_empty()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateSeries(evaluationId, SearchSource.Feed, []), TestActor);

        var completed = ExpectMsg<EvaluateSeriesCompleted>();
        Assert.Multiple(
            () => Assert.Empty(completed.Items),
            () => Assert.Equal(0, completed.Total));
        p.Resolver.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void Feed_evaluation_with_tvdb_id_enriches_matched_items()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateSeries(evaluationId, SearchSource.Feed,
        [
            new MediathekItem("SWR", "Tatort", "Roomservice", null, 1788113728, 5322, 1400000000,
                null, "https://x.com/v.mp4", null, null, null)
        ], TvdbId: 83214), TestActor);

        var resolve = p.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Equal("Tatort", resolve.TopicOrAlias);
        p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

        var score = p.Scoring.ExpectMsg<ScoreItems>();
        Assert.Equal(SearchSource.Feed, score.Origin.Source);
        p.Scoring.Reply(new ScoreCompleted(Guid.Empty,
        [
            new ScoredItem(0, 0.9, true, new MetadataSpec(null, null, DateTimeOffset.FromUnixTimeSeconds(1788113728)))
        ], []));

        var enrichRequest = p.Enrichment.ExpectMsg<EnrichEpisodes>();
        Assert.Multiple(
            () => Assert.Equal(83214, enrichRequest.TvdbId),
            () => Assert.Null(enrichRequest.Season),
            () => Assert.Equal("Roomservice", Assert.Single(enrichRequest.Candidates).Title));
        p.Enrichment.Reply(new EnrichEpisodesCompleted(
        [
            new EnrichedEpisode(0, "2026", "09", "Roomservice", 0.85f, MatchMethod.TitleMatch)
        ]));

        var result = ExpectMsg<EvaluateSeriesCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Multiple(
            () => Assert.Contains("S2026E09", item.Title),
            () => Assert.Equal("2026", item.Metadata?.Season),
            () => Assert.Equal("09", item.Metadata?.Episode),
            () => Assert.Equal(0.85f, item.Metadata?.MatchConfidence),
            () => Assert.Equal(MatchMethod.TitleMatch, item.Metadata?.MatchMethod));
    }

    [Fact]
    public void Resolution_runs_for_items_with_regex_season_episode()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateSeries(evaluationId, SearchSource.Sonarr,
        [
            new MediathekItem("ZDF", "ZDF Magazin Royale", "Episode Title (S2026/E01)", null,
                1788113728, 1835, 600000000, null, "https://x.com/v.mp4", null, null, null)
        ], TvdbId: 390284, Season: 2026, RuleSet: new RuleSetResolved("zdf-magazin-royale", "ZDF Magazin Royale")), TestActor);

        p.Scoring.ExpectMsg<ScoreItems>();
        p.Scoring.Reply(new ScoreCompleted(Guid.Empty,
        [
            new ScoredItem(0, 0.95, true, new MetadataSpec("2026", "01", DateTimeOffset.FromUnixTimeSeconds(1788113728)))
        ], []));

        var enrichMsg = p.Enrichment.ExpectMsg<EnrichEpisodes>();
        var candidate = Assert.Single(enrichMsg.Candidates);
        Assert.Multiple(
            () => Assert.Equal(2026, enrichMsg.Season),
            () => Assert.Equal("2026", candidate.ExistingSeason),
            () => Assert.Equal("01", candidate.ExistingEpisode));

        p.Enrichment.Reply(new EnrichEpisodesCompleted(
        [
            new EnrichedEpisode(0, "2026", "1", "Episode Title", 0.9f, MatchMethod.AirdateMatch)
        ]));

        var result = ExpectMsg<EvaluateSeriesCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Multiple(
            () => Assert.Equal("2026", item.Metadata?.Season),
            () => Assert.Equal("1", item.Metadata?.Episode));
    }

    [Fact]
    public void Enrichment_failure_falls_back_to_unresolved_results()
    {
        var p = RegisterProbes();
        var evaluationId = Guid.NewGuid();
        var worker = CreateWorker(evaluationId);

        worker.Tell(new EvaluateSeries(evaluationId, SearchSource.Sonarr,
        [
            new MediathekItem("SWR", "Tatort", "Roomservice", null, 1788113728, 5322, 1400000000,
                null, "https://x.com/v.mp4", null, null, null)
        ], TvdbId: 83214, Season: 2026, RuleSet: new RuleSetResolved("tatort", "Tatort")), TestActor);

        p.Scoring.ExpectMsg<ScoreItems>();
        p.Scoring.Reply(new ScoreCompleted(Guid.Empty,
        [
            new ScoredItem(0, 0.9, true, new MetadataSpec(null, null, DateTimeOffset.FromUnixTimeSeconds(1788113728)))
        ], []));

        p.Enrichment.ExpectMsg<EnrichEpisodes>();
        p.Enrichment.Reply(new EnrichEpisodesFailed(new Exception("KEKW")));

        var result = ExpectMsg<EvaluateSeriesCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Multiple(
            () => Assert.Null(item.Metadata?.Season),
            () => Assert.Null(item.Metadata?.MatchConfidence));
    }

    private static MediathekItem Item(string topic, string title, string channel = "ARD") =>
        new(channel, topic, title, null, 1761800000, 5400, 0, null, "https://example.com/v.mp4", null, null, null);
}
