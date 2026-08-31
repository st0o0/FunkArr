using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;
using FunkArr.Search;
using Xunit;

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
        registry.Register<IMediathekGateway>(mediathekProbe);
        registry.Register<IMatchMagicService>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, "Das Boot", null, null), TestActor);

        var mediathekQuery = mediathekProbe.ExpectMsg<MediathekQuery>();
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
        resolverProbe.Reply(new RuleSetResolved("das-boot"));

        var scoreRequest = matchMagicProbe.ExpectMsg<ScoreItems>();
        Assert.Equal("das-boot", scoreRequest.RuleSetId);

        matchMagicProbe.Reply(new ScoreCompleted([new ScoredItem(0, 0.8, true)]));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Single(result.Items);
    }

    [Fact]
    public void No_query_returns_search_failed()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekGateway>(mediathekProbe);
        registry.Register<IMatchMagicService>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, null, null, null), TestActor);

        var result = ExpectMsg<SearchFailed>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Contains("requires a query", result.Reason);
    }

    [Fact]
    public void RuleSet_not_found_returns_unscored_results()
    {
        var mediathekProbe = CreateTestProbe();
        var matchMagicProbe = CreateTestProbe();
        var resolverProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekGateway>(mediathekProbe);
        registry.Register<IMatchMagicService>(matchMagicProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, "Unknown Movie", null, null), TestActor);

        mediathekProbe.ExpectMsg<MediathekQuery>();
        mediathekProbe.Reply(new MediathekQueryCompleted(
        [
            new MediathekItem("ZDF", "Unknown Movie", "Unknown Movie", null, 0, 7200, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        resolverProbe.ExpectMsg<ResolveRuleSet>();
        resolverProbe.Reply(new RuleSetNotFound("Unknown Movie"));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Single(result.Items);
        Assert.Equal(0.0, result.Items[0].Score);
    }
}
