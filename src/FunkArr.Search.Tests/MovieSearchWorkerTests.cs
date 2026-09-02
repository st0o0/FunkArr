using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class MovieSearchWorkerTests : TestKit
{
    [Fact]
    public void Successful_movie_search()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, "Das Boot", null, null, null, null), TestActor);

        var mediathekQuery = mediathekProbe.ExpectMsg<QueryMediathek>();
        Assert.Contains("title", mediathekQuery.Fields[0].Fields);
        Assert.Contains("topic", mediathekQuery.Fields[0].Fields);
        Assert.Equal(3600, mediathekQuery.DurationMin);

        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Das Boot", "Das Boot", null, 1719244800, 7200, 2400000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null),
        ], 1));

        var resolveRequest = resolverProbe.ExpectMsg<ResolveRuleSet>();
        Assert.Equal("Das Boot", resolveRequest.TopicOrAlias);
        resolverProbe.Reply(new RuleSetResolved("das-boot", "Das Boot"));

        var scoreRequest = matchMagicProbe.ExpectMsg<ScoreItems>();
        Assert.Equal("das-boot", scoreRequest.RuleSetId);

        matchMagicProbe.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.8, true)]));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Single(result.Items);
    }

    [Fact]
    public void No_query_queries_mediathek_for_recent_items()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, null, null, null, null, null), TestActor);

        var query = mediathekProbe.ExpectMsg<QueryMediathek>();
        Assert.Empty(query.Fields);
        mediathekProbe.Reply(new MediathekQueryCompleted([], 0));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Empty(result.Items);
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

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, "Unknown Movie", null, null, null, null), TestActor);

        mediathekProbe.ExpectMsg<QueryMediathek>();
        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ZDF", "Unknown Movie", "Unknown Movie", null, 0, 7200, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        resolverProbe.ExpectMsg<ResolveRuleSet>();
        resolverProbe.Reply(new RuleSetNotFound("Unknown Movie"));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        var item = Assert.Single(result.Items);
        Assert.Equal(0.0, item.Score);
    }

    [Fact]
    public void ImdbId_only_search_resolves_then_queries_mvw()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, null, "tt0806910", null, null, null), TestActor);

        var resolveRequest = resolverProbe.ExpectMsg<ResolveRuleSet>();
        Assert.Null(resolveRequest.TopicOrAlias);
        Assert.Equal("tt0806910", resolveRequest.ImdbId);
        resolverProbe.Reply(new RuleSetResolved("tatort", "Tatort"));

        var mediathekQuery = mediathekProbe.ExpectMsg<QueryMediathek>();
        Assert.Contains("Tatort", mediathekQuery.Fields[0].Query);
        Assert.Equal(3600, mediathekQuery.DurationMin);

        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Der Film", null, 1719244800, 7200, 2400000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null),
        ], 1));

        var scoreRequest = matchMagicProbe.ExpectMsg<ScoreItems>();
        Assert.Equal("tatort", scoreRequest.RuleSetId);

        matchMagicProbe.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.85, true)]));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Single(result.Items);
        Assert.Equal("tt0806910", result.Items[0].ImdbId);
    }

    [Fact]
    public void TmdbId_only_search_resolves_then_queries_mvw()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, null, null, 550, null, null), TestActor);

        var resolveRequest = resolverProbe.ExpectMsg<ResolveRuleSet>();
        Assert.Equal(550, resolveRequest.TmdbId);
        resolverProbe.Reply(new RuleSetResolved("fight-club", "Fight Club"));

        mediathekProbe.ExpectMsg<QueryMediathek>();
        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Fight Club", "Fight Club", null, 0, 7200, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        matchMagicProbe.ExpectMsg<ScoreItems>();
        matchMagicProbe.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.7, true)]));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Single(result.Items);
        Assert.Equal(550, result.Items[0].TmdbId);
    }

    [Fact]
    public void ImdbId_only_unresolved_returns_empty()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, null, "tt9999999", null, null, null), TestActor);

        resolverProbe.ExpectMsg<ResolveRuleSet>();
        resolverProbe.Reply(new RuleSetNotFound(""));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void Text_search_carries_imdbId_through_to_results()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(mediathekProbe);
        registry.Register<IMatchMagicManager>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, "Das Boot", "tt1234567", null, null, null), TestActor);

        mediathekProbe.ExpectMsg<QueryMediathek>();
        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ARD", "Das Boot", "Das Boot", null, 0, 7200, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        resolverProbe.ExpectMsg<ResolveRuleSet>();
        resolverProbe.Reply(new RuleSetNotFound("Das Boot"));

        var result = ExpectMsg<SearchCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Equal("tt1234567", item.ImdbId);
    }
}
