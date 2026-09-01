using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.Search;
using FunkArr.Search;
using Xunit;

namespace FunkArr.Search.Tests;

public sealed class SearchManagerTests : TestKit
{
    private IActorRef CreateGateway(TestProbe tvProbe, TestProbe movieProbe)
    {
        var registry = ActorRegistry.For(Sys);
        registry.Register<ITvSearchRegion>(tvProbe, overwrite: true);
        registry.Register<IMovieSearchRegion>(movieProbe, overwrite: true);
        return Sys.ActorOf(Props.Create(() => new SearchManager()));
    }

    [Fact]
    public void Routes_tv_search_to_tv_shard_region()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new TvSearchCommand(Guid.Empty, "Tatort", null, null, null, null, null, null), TestActor);

        var forwarded = tvProbe.ExpectMsg<TvSearchCommand>();
        Assert.Equal("Tatort", forwarded.Query);
        Assert.NotEqual(Guid.Empty, forwarded.SearchId);

        movieProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Routes_movie_search_to_movie_shard_region()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new MovieSearchCommand(Guid.Empty, "Das Boot", null, null, null, null), TestActor);

        var forwarded = movieProbe.ExpectMsg<MovieSearchCommand>();
        Assert.Equal("Das Boot", forwarded.Query);

        tvProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void General_search_with_tv_cat_routes_to_tv()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new GeneralSearchCommand("test", 5040, null, null), TestActor);

        tvProbe.ExpectMsg<TvSearchCommand>();
        movieProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void General_search_with_movie_cat_routes_to_movie()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new GeneralSearchCommand("test", 2040, null, null), TestActor);

        movieProbe.ExpectMsg<MovieSearchCommand>();
        tvProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void General_search_without_cat_fans_out_to_both()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new GeneralSearchCommand("test", null, null, null), TestActor);

        tvProbe.ExpectMsg<TvSearchCommand>();
        movieProbe.ExpectMsg<MovieSearchCommand>();
    }

    [Fact]
    public void Fan_out_merges_both_results()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new GeneralSearchCommand("test", null, null, null), TestActor);

        var tvCmd = tvProbe.ExpectMsg<TvSearchCommand>();
        var movieCmd = movieProbe.ExpectMsg<MovieSearchCommand>();

        var tvItem = new SearchResultItem("TV Show", "ARD", "Topic", "url", 5400, 100, 720, null, 0.9);
        var movieItem = new SearchResultItem("Movie", "ZDF", "Film", "url2", 7200, 200, 1080, null, 0.8);

        gateway.Tell(new SearchCompleted(tvCmd.SearchId, [tvItem], 1));

        ExpectNoMsg(TimeSpan.FromMilliseconds(100));

        gateway.Tell(new SearchCompleted(movieCmd.SearchId, [movieItem], 1));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Equal(2, result.Items.Length);
        Assert.Equal("TV Show", result.Items[0].Title);
        Assert.Equal("Movie", result.Items[1].Title);
    }

    [Fact]
    public void Single_search_forwards_result()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new TvSearchCommand(Guid.Empty, "Tatort", null, null, null, null, null, null), TestActor);

        var tvCmd = tvProbe.ExpectMsg<TvSearchCommand>();
        var item = new SearchResultItem("Tatort: Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.95);

        gateway.Tell(new SearchCompleted(tvCmd.SearchId, [item], 1));

        var result = ExpectMsg<SearchCompleted>();
        Assert.Single(result.Items);
    }

    [Fact]
    public void Failure_forwarded_to_sender()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new TvSearchCommand(Guid.Empty, "Tatort", null, null, null, null, null, null), TestActor);

        var tvCmd = tvProbe.ExpectMsg<TvSearchCommand>();
        gateway.Tell(new SearchFailed(tvCmd.SearchId, "Error"));

        var result = ExpectMsg<SearchFailed>();
        Assert.Contains("Error", result.Reason);
    }

    [Fact]
    public void Timeout_responds_with_search_failed()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<ITvSearchRegion>(tvProbe, overwrite: true);
        registry.Register<IMovieSearchRegion>(movieProbe, overwrite: true);
        var gateway = Sys.ActorOf(Props.Create(() =>
            new SearchManager(TimeSpan.FromMilliseconds(200))));

        gateway.Tell(new TvSearchCommand(Guid.Empty, "Tatort", null, null, null, null, null, null), TestActor);

        tvProbe.ExpectMsg<TvSearchCommand>();

        var result = ExpectMsg<SearchFailed>(TimeSpan.FromSeconds(5));
        Assert.Contains("timed out", result.Reason);
    }
}
