using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;
using Microsoft.Extensions.Options;

namespace FunkArr.Search.Tests;

public sealed class MovieSearchWorkerTests : TestKit
{
    private sealed record TestProbes(
        Akka.TestKit.TestProbe Mediathek,
        Akka.TestKit.TestProbe Resolver,
        Akka.TestKit.TestProbe Evaluation);

    private TestProbes RegisterProbes()
    {
        var probes = new TestProbes(CreateTestProbe(), CreateTestProbe(), CreateTestProbe());
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(probes.Mediathek);
        registry.Register<IRuleSetResolver>(probes.Resolver);
        registry.Register<IMovieEvaluationRegion>(probes.Evaluation);
        return probes;
    }

    private IActorRef CreateWorker(Guid searchId, bool includeFuture = false)
    {
        IOptionsMonitor<SearchOptions> options = new StaticOptionsMonitor<SearchOptions>(new SearchOptions { IncludeFuture = includeFuture });
        return Sys.ActorOf(Props.Create(() => new MovieSearchWorker(searchId.ToString(), options)));
    }

    [Fact]
    public void Text_search_queries_title_and_topic_and_delegates_evaluation()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchMovie(searchId, SearchSource.Radarr, "Das Boot", null, null, null, null), TestActor);

        var query = p.Mediathek.ExpectMsg<QueryMediathek>();
        var field = Assert.Single(query.Fields);
        Assert.Multiple(
            () => Assert.Equal(new[] { "title", "topic" }, field.Fields),
            () => Assert.Equal("Das Boot", field.Query),
            () => Assert.Equal(3600, query.DurationMin));
        p.Mediathek.Reply(new QueryMediathekCompleted([Item("Das Boot")], 1));

        var evaluation = p.Evaluation.ExpectMsg<EvaluateMovies>();
        Assert.Multiple(
            () => Assert.Equal(SearchSource.Radarr, evaluation.Source),
            () => Assert.Equal("Das Boot", Assert.Single(evaluation.Items).Title),
            () => Assert.Null(evaluation.RuleSet));
        var resultItem = new SearchResultItem("Das.Boot.1981.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Das Boot",
            "https://x.com/v.mp4", 7200, 0, 720, null, 0.9);
        p.Evaluation.Reply(new EvaluateMoviesCompleted(evaluation.SearchId, [resultItem], 1));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Equal(resultItem, Assert.Single(result.Items)));
    }

    [Fact]
    public void No_query_queries_mediathek_for_recent_items()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchMovie(searchId, SearchSource.Radarr, null, null, null, null, null), TestActor);

        var query = p.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Empty(query.Fields);
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Empty(result.Items));
    }

    [Fact]
    public void Imdb_id_search_resolves_ruleset_and_passes_ids_to_evaluation()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchMovie(searchId, SearchSource.Radarr, null, "tt0806910", 550, null, null), TestActor);

        var resolve = p.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Multiple(
            () => Assert.Null(resolve.TopicOrAlias),
            () => Assert.Equal("tt0806910", resolve.Ids?.ImdbId),
            () => Assert.Equal(550, resolve.Ids?.TmdbId));
        var ruleSet = new RuleSetResolved("tatort", "Tatort");
        p.Resolver.Reply(ruleSet);

        var query = p.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Equal("Tatort", Assert.Single(query.Fields).Query);
        p.Mediathek.Reply(new QueryMediathekCompleted([Item("Tatort")], 1));

        var evaluation = p.Evaluation.ExpectMsg<EvaluateMovies>();
        Assert.Multiple(
            () => Assert.Equal(ruleSet, evaluation.RuleSet),
            () => Assert.Equal("tt0806910", evaluation.ImdbId),
            () => Assert.Equal(550, evaluation.TmdbId));
    }

    [Fact]
    public void Imdb_id_unresolved_returns_empty()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchMovie(searchId, SearchSource.Radarr, null, "tt9999999", null, null, null), TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetFailed(new RuleSetNotFoundException("")));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Multiple(
            () => Assert.Empty(result.Items),
            () => Assert.Equal(0, result.Total));
        p.Mediathek.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Include_future_sends_a_second_query_with_future_enabled()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId, includeFuture: true);

        worker.Tell(new SearchMovie(searchId, SearchSource.Radarr, "Das Boot", null, null, null, null), TestActor);

        var first = p.Mediathek.ExpectMsg<QueryMediathek>();
        var second = p.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Multiple(
            () => Assert.Equal(new[] { false, true }, new[] { first.Future, second.Future }.Order().ToArray()),
            () => Assert.Equal(first with { Future = second.Future }, second));
    }

    [Fact]
    public void Evaluation_failure_returns_search_failed()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchMovie(searchId, SearchSource.Radarr, "Das Boot", null, null, null, null), TestActor);

        p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new QueryMediathekCompleted([Item("Das Boot")], 1));
        var evaluation = p.Evaluation.ExpectMsg<EvaluateMovies>();
        p.Evaluation.Reply(new EvaluateMoviesFailed(evaluation.SearchId, new Exception("Scoring error")));

        var result = ExpectMsg<SearchMovieFailed>();
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Contains("Scoring error", result.Cause.Message));
    }

    private static MediathekItem Item(string title) =>
        new("ARD", title, title, null, 1719244800, 7200, 0, null, "https://x.com/v.mp4", null, null, null);
}
