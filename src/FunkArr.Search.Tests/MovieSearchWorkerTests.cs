using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;
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
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekGateway>(mediathekProbe);
        registry.Register<IMatchMagicService>(matchMagicProbe);

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

        matchMagicProbe.ExpectMsg<ScoreItems>();
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
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekGateway>(mediathekProbe);
        registry.Register<IMatchMagicService>(matchMagicProbe);

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new MovieSearchCommand(searchId, null, null, null), TestActor);

        var result = ExpectMsg<SearchFailed>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Contains("requires a query", result.Reason);
    }
}
