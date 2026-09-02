using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class TvSearchWorkerTests : TestKit
{
    [Fact]
    public void Successful_search_returns_scored_results()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Tatort", null, null, null, null, null, null), TestActor);

        var mediathekQuery = mediathekProbe.ExpectMsg<QueryMediathek>();
        Assert.Equal("topic", mediathekQuery.Fields[0].Fields[0]);
        Assert.Equal("Tatort", mediathekQuery.Fields[0].Query);
        Assert.Equal(300, mediathekQuery.DurationMin);

        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Test", null, 1719244800, 5400, 1200000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null),
        ], 1));

        var resolveRequest = resolverProbe.ExpectMsg<ResolveRuleSet>();
        Assert.Equal("Tatort", resolveRequest.TopicOrAlias);
        resolverProbe.Reply(new RuleSetResolved("tatort", "Tatort"));

        var scoreRequest = matchMagicProbe.ExpectMsg<ScoreItems>();
        Assert.Single(scoreRequest.Candidates);
        Assert.Equal("tatort", scoreRequest.RuleSetId);

        matchMagicProbe.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.95, true)]));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Single(result.Items);
        Assert.Equal("Tatort: Test", result.Items[0].Title);
        Assert.Equal(0.95, result.Items[0].Score);
    }

    [Fact]
    public void Mediathek_failure_returns_search_failed()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Tatort", null, null, null, null, null, null), TestActor);

        mediathekProbe.ExpectMsg<QueryMediathek>();
        mediathekProbe.Reply(new MediathekQueryFailed("Connection refused"));

        var result = ExpectMsg<SearchFailed>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Contains("Connection refused", result.Reason);
    }

    [Fact]
    public void Scoring_failure_returns_search_failed()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Tatort", null, null, null, null, null, null), TestActor);

        mediathekProbe.ExpectMsg<QueryMediathek>();
        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Test", null, 0, 5400, 0, null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        resolverProbe.ExpectMsg<ResolveRuleSet>();
        resolverProbe.Reply(new RuleSetResolved("tatort", "Tatort"));

        matchMagicProbe.ExpectMsg<ScoreItems>();
        matchMagicProbe.Reply(new Status.Failure(new Exception("Scoring error")));

        var result = ExpectMsg<SearchFailed>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Contains("Scoring error", result.Reason);
    }

    [Fact]
    public void RuleSet_not_found_returns_unscored_results()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Unknown Show", null, null, null, null, null, null), TestActor);

        mediathekProbe.ExpectMsg<QueryMediathek>();
        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ZDF", "Unknown Show", "Episode 1", null, 0, 3600, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        resolverProbe.ExpectMsg<ResolveRuleSet>();
        resolverProbe.Reply(new RuleSetNotFound("Unknown Show"));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        var item = Assert.Single(result.Items);
        Assert.Equal(0.0, item.Score);
    }

    [Fact]
    public void TvdbId_only_search_resolves_then_queries_mvw()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, null, null, null, 83214, null, null, null), TestActor);

        var resolveRequest = resolverProbe.ExpectMsg<ResolveRuleSet>();
        Assert.Null(resolveRequest.TopicOrAlias);
        Assert.Equal(83214, resolveRequest.TvdbId);
        resolverProbe.Reply(new RuleSetResolved("tatort", "Tatort"));

        var mediathekQuery = mediathekProbe.ExpectMsg<QueryMediathek>();
        Assert.Equal("Tatort", mediathekQuery.Fields[0].Query);

        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Test", null, 1719244800, 5400, 1200000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null),
        ], 1));

        var scoreRequest = matchMagicProbe.ExpectMsg<ScoreItems>();
        Assert.Equal("tatort", scoreRequest.RuleSetId);

        matchMagicProbe.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.9, true)]));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Single(result.Items);
        Assert.Equal(83214, result.Items[0].TvdbId);
    }

    [Fact]
    public void TvdbId_only_search_unresolved_returns_empty()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, null, null, null, 99999, null, null, null), TestActor);

        resolverProbe.ExpectMsg<ResolveRuleSet>();
        resolverProbe.Reply(new RuleSetNotFound(""));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void Text_search_carries_tvdbId_through_to_results()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new TvSearchCommand(searchId, "Tatort", null, null, 83214, null, null, null), TestActor);

        mediathekProbe.ExpectMsg<QueryMediathek>();
        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Test", null, 0, 5400, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        resolverProbe.ExpectMsg<ResolveRuleSet>();
        resolverProbe.Reply(new RuleSetNotFound("Tatort"));

        var result = ExpectMsg<SearchCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Equal(83214, item.TvdbId);
    }
}
