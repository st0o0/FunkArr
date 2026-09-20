using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Search;

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

        gateway.Tell(new SearchCommand(SearchSource.Sonarr, "Tatort", null, null, null,
            new SearchCommand.TvParams(null, null, null, null)), TestActor);

        var forwarded = tvProbe.ExpectMsg<SearchSeries>();
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

        gateway.Tell(new SearchCommand(SearchSource.Radarr, "Das Boot", null, null, null,
            new SearchCommand.MovieParams(null, null)), TestActor);

        var forwarded = movieProbe.ExpectMsg<SearchMovie>();
        Assert.Equal("Das Boot", forwarded.Query);

        tvProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void General_search_with_tv_cat_routes_to_tv()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new SearchCommand(SearchSource.Prowlarr, "test", 5040, null, null, null), TestActor);

        tvProbe.ExpectMsg<SearchSeries>();
        movieProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void General_search_with_movie_cat_routes_to_movie()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new SearchCommand(SearchSource.Prowlarr, "test", 2040, null, null, null), TestActor);

        movieProbe.ExpectMsg<SearchMovie>();
        tvProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void General_search_without_cat_fans_out_to_both()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new SearchCommand(SearchSource.Prowlarr, "test", null, null, null, null), TestActor);

        tvProbe.ExpectMsg<SearchSeries>();
        movieProbe.ExpectMsg<SearchMovie>();
    }

    [Fact]
    public void Fan_out_merges_both_results()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new SearchCommand(SearchSource.Prowlarr, "test", null, null, null, null), TestActor);

        var tvCmd = tvProbe.ExpectMsg<SearchSeries>();
        var movieCmd = movieProbe.ExpectMsg<SearchMovie>();

        var tvItem = new SearchResultItem("TV Show", "ARD", "Topic", "url", 5400, 100, 720, null, 0.9);
        var movieItem = new SearchResultItem("Movie", "ZDF", "Film", "url2", 7200, 200, 1080, null, 0.8);

        gateway.Tell(new SearchSeriesCompleted(tvCmd.SearchId, [tvItem], 1));

        ExpectNoMsg(TimeSpan.FromMilliseconds(100));

        gateway.Tell(new SearchMovieCompleted(movieCmd.SearchId, [movieItem], 1));

        var result = ExpectMsg<SearchCommandCompleted>();
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

        gateway.Tell(new SearchCommand(SearchSource.Sonarr, "Tatort", null, null, null,
            new SearchCommand.TvParams(null, null, null, null)), TestActor);

        var tvCmd = tvProbe.ExpectMsg<SearchSeries>();
        var item = new SearchResultItem("Tatort: Test", "ARD", "Tatort", "url", 5400, 100, 720, null, 0.95);

        gateway.Tell(new SearchSeriesCompleted(tvCmd.SearchId, [item], 1));

        var result = ExpectMsg<SearchCommandCompleted>();
        Assert.Single(result.Items);
    }

    [Fact]
    public void Failure_forwarded_to_sender()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new SearchCommand(SearchSource.Sonarr, "Tatort", null, null, null,
            new SearchCommand.TvParams(null, null, null, null)), TestActor);

        var tvCmd = tvProbe.ExpectMsg<SearchSeries>();
        gateway.Tell(new SearchSeriesFailed(tvCmd.SearchId, new Exception("Error")));

        var result = ExpectMsg<SearchCommandFailed>();
        Assert.Contains("Error", result.Cause.Message);
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

        gateway.Tell(new SearchCommand(SearchSource.Sonarr, "Tatort", null, null, null,
            new SearchCommand.TvParams(null, null, null, null)), TestActor);

        tvProbe.ExpectMsg<SearchSeries>();

        var result = ExpectMsg<SearchCommandFailed>(TimeSpan.FromSeconds(5));
        Assert.Contains("timed out", result.Cause.Message);
    }

    [Fact]
    public void Fan_out_partial_success_returns_partial_when_other_fails()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new SearchCommand(SearchSource.Prowlarr, "test", null, null, null, null), TestActor);

        var tvCmd = tvProbe.ExpectMsg<SearchSeries>();
        var movieCmd = movieProbe.ExpectMsg<SearchMovie>();

        var tvItem = new SearchResultItem("TV Show", "ARD", "Topic", "url", 5400, 100, 720, null, 0.9);
        gateway.Tell(new SearchSeriesCompleted(tvCmd.SearchId, [tvItem], 1));

        ExpectNoMsg(TimeSpan.FromMilliseconds(100));

        gateway.Tell(new SearchMovieFailed(movieCmd.SearchId, new Exception("Movie search failed")));

        var result = ExpectMsg<SearchCommandCompleted>();
        Assert.Single(result.Items);
        Assert.Equal("TV Show", result.Items[0].Title);
    }

    [Fact]
    public void Fan_out_both_fail_returns_first_failure()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var gateway = CreateGateway(tvProbe, movieProbe);

        gateway.Tell(new SearchCommand(SearchSource.Prowlarr, "test", null, null, null, null), TestActor);

        var tvCmd = tvProbe.ExpectMsg<SearchSeries>();
        var movieCmd = movieProbe.ExpectMsg<SearchMovie>();

        gateway.Tell(new SearchSeriesFailed(tvCmd.SearchId, new Exception("TV failed")));

        var result = ExpectMsg<SearchCommandFailed>();
        Assert.Contains("TV failed", result.Cause.Message);

        gateway.Tell(new SearchMovieFailed(movieCmd.SearchId, new Exception("Movie failed")));
        ExpectNoMsg(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void Fan_out_timeout_with_partial_returns_partial()
    {
        var tvProbe = CreateTestProbe();
        var movieProbe = CreateTestProbe();
        var registry = ActorRegistry.For(Sys);
        registry.Register<ITvSearchRegion>(tvProbe, overwrite: true);
        registry.Register<IMovieSearchRegion>(movieProbe, overwrite: true);
        var gateway = Sys.ActorOf(Props.Create(() =>
            new SearchManager(TimeSpan.FromMilliseconds(200))));

        gateway.Tell(new SearchCommand(SearchSource.Prowlarr, "test", null, null, null, null), TestActor);

        var tvCmd = tvProbe.ExpectMsg<SearchSeries>();
        movieProbe.ExpectMsg<SearchMovie>();

        var tvItem = new SearchResultItem("TV Show", "ARD", "Topic", "url", 5400, 100, 720, null, 0.9);
        gateway.Tell(new SearchSeriesCompleted(tvCmd.SearchId, [tvItem], 1));

        var result = ExpectMsg<SearchCommandCompleted>(TimeSpan.FromSeconds(5));
        Assert.Single(result.Items);
        Assert.Equal("TV Show", result.Items[0].Title);
    }
}
