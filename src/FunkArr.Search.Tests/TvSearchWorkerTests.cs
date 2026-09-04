using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.MetadataResolver;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class TvSearchWorkerTests : TestKit
{
    private record TestProbes(
        Akka.TestKit.TestProbe Mediathek,
        Akka.TestKit.TestProbe MatchMagic,
        Akka.TestKit.TestProbe Resolver,
        Akka.TestKit.TestProbe MetadataResolver);

    private TestProbes RegisterProbes()
    {
        var probes = new TestProbes(
            CreateTestProbe(),
            CreateTestProbe(),
            CreateTestProbe(),
            CreateTestProbe());

        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(probes.Mediathek);
        registry.Register<IMatchMagicManager>(probes.MatchMagic);
        registry.Register<IRuleSetResolver>(probes.Resolver);
        registry.Register<IMetadataResolver>(probes.MetadataResolver);

        return probes;
    }

    [Fact]
    public void Successful_search_returns_scored_results()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Tatort", null, null, null, null, null, null), TestActor);

        var mediathekQuery = p.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Equal("topic", mediathekQuery.Fields[0].Fields[0]);
        Assert.Equal("Tatort", mediathekQuery.Fields[0].Query);
        Assert.Equal(300, mediathekQuery.DurationMin);

        p.Mediathek.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Test", null, 1719244800, 5400, 1200000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null),
        ], 1));

        var resolveRequest = p.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Equal("Tatort", resolveRequest.TopicOrAlias);
        p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

        var scoreRequest = p.MatchMagic.ExpectMsg<ScoreItems>();
        Assert.Single(scoreRequest.Candidates);
        Assert.Equal("tatort", scoreRequest.RuleSetId);

        p.MatchMagic.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.95, true)]));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Single(result.Items);
        Assert.Equal("Tatort.Tatort.Test.GERMAN.720p.WEB.h264-FunkArr", result.Items[0].Title);
        Assert.Equal(0.95, result.Items[0].Score);
    }

    [Fact]
    public void Mediathek_failure_returns_search_failed()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Tatort", null, null, null, null, null, null), TestActor);

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new MediathekQueryFailed("Connection refused"));

        var result = ExpectMsg<SearchFailed>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Contains("Connection refused", result.Reason);
    }

    [Fact]
    public void Scoring_failure_returns_search_failed()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Tatort", null, null, null, null, null, null), TestActor);

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Test", null, 0, 5400, 0, null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

        p.MatchMagic.ExpectMsg<ScoreItems>();
        p.MatchMagic.Reply(new Status.Failure(new Exception("Scoring error")));

        var result = ExpectMsg<SearchFailed>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Contains("Scoring error", result.Reason);
    }

    [Fact]
    public void RuleSet_not_found_returns_unscored_results()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Unknown Show", null, null, null, null, null, null), TestActor);

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ZDF", "Unknown Show", "Episode 1", null, 0, 3600, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetNotFound("Unknown Show"));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        var item = Assert.Single(result.Items);
        Assert.Equal(0.0, item.Score);
    }

    [Fact]
    public void TvdbId_only_search_resolves_then_queries_mvw()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, null, null, null, 83214, null, null, null), TestActor);

        var resolveRequest = p.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Null(resolveRequest.TopicOrAlias);
        Assert.Equal(83214, resolveRequest.TvdbId);
        p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

        var mediathekQuery = p.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Equal("Tatort", mediathekQuery.Fields[0].Query);

        p.Mediathek.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Test", null, 1719244800, 5400, 1200000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null),
        ], 1));

        var scoreRequest = p.MatchMagic.ExpectMsg<ScoreItems>();
        Assert.Equal("tatort", scoreRequest.RuleSetId);

        p.MatchMagic.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.9, true)]));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Single(result.Items);
        Assert.Equal(83214, result.Items[0].TvdbId);
    }

    [Fact]
    public void TvdbId_only_search_unresolved_returns_empty()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, null, null, null, 99999, null, null, null), TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetNotFound(""));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void Text_search_carries_tvdbId_through_to_results()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Tatort", null, null, 83214, null, null, null), TestActor);

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Test", null, 0, 5400, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetNotFound("Tatort"));

        var result = ExpectMsg<SearchCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Equal(83214, item.TvdbId);
    }

    [Fact]
    public void Resolution_triggered_for_matched_items_without_season_episode()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, null, 2026, null, 83214, null, null, null), TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("SWR", "Tatort", "Roomservice", null, 1788113728, 5322, 1400000000,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        p.MatchMagic.ExpectMsg<ScoreItems>();
        p.MatchMagic.Reply(new ScoreCompleted(Guid.Empty,
        [
            new ScoredItem(0, 0.9, true, new MetadataSpec(null, null,
                DateTimeOffset.FromUnixTimeSeconds(1788113728))),
        ]));

        var resolveRequest = p.MetadataResolver.ExpectMsg<ResolveEpisodes>();
        Assert.Equal(83214, resolveRequest.TvdbId);
        Assert.Equal(2026, resolveRequest.Season);
        Assert.Single(resolveRequest.Candidates);
        Assert.Equal("Roomservice", resolveRequest.Candidates[0].Title);

        p.MetadataResolver.Reply(new EpisodesResolved(
        [
            new ResolvedEpisode(0, "2026", "09", "Sashimi Spezial", 0.85f, "FuzzyTitleMatch"),
        ]));

        var result = ExpectMsg<SearchCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Contains("S2026E09", item.Title);
        Assert.Equal("2026", item.Season);
        Assert.Equal("09", item.Episode);
        Assert.Equal(0.85f, item.ResolutionConfidence);
        Assert.Equal("FuzzyTitleMatch", item.ResolutionStrategy);
    }

    [Fact]
    public void Resolution_skipped_when_items_have_season_episode()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, null, 2026, null, 390284, null, null, null), TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetResolved("zdf-magazin-royale", "ZDF Magazin Royale"));

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ZDF", "ZDF Magazin Royale", "Episode Title (S2026/E01)", null,
                1788113728, 1835, 600000000, null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        p.MatchMagic.ExpectMsg<ScoreItems>();
        p.MatchMagic.Reply(new ScoreCompleted(Guid.Empty,
        [
            new ScoredItem(0, 0.95, true, new MetadataSpec("2026", "01",
                DateTimeOffset.FromUnixTimeSeconds(1788113728))),
        ]));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Single(result.Items);
        Assert.Equal("2026", result.Items[0].Season);
        Assert.Equal("01", result.Items[0].Episode);

        p.MetadataResolver.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void Resolution_failure_falls_back_to_unresolved_results()
    {
        var p = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, null, 2026, null, 83214, null, null, null), TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("SWR", "Tatort", "Roomservice", null, 1788113728, 5322, 1400000000,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        p.MatchMagic.ExpectMsg<ScoreItems>();
        p.MatchMagic.Reply(new ScoreCompleted(Guid.Empty,
        [
            new ScoredItem(0, 0.9, true, new MetadataSpec(null, null,
                DateTimeOffset.FromUnixTimeSeconds(1788113728))),
        ]));

        p.MetadataResolver.ExpectMsg<ResolveEpisodes>();
        p.MetadataResolver.Reply(new EpisodeResolutionFailed("TVDB unavailable"));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Single(result.Items);
        Assert.Null(result.Items[0].Season);
        Assert.Null(result.Items[0].ResolutionConfidence);
    }
}
