using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;

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

    private IActorRef CreateWorker(Guid searchId) =>
        Sys.ActorOf(Props.Create(() => new MovieSearchWorker(searchId.ToString())));

    [Fact]
    public void Text_search_queries_title_and_topic_and_delegates_evaluation()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchMovie(searchId, SearchSource.Radarr, "Das Boot", null, null, null, null), TestActor);

        var query = AnswerQuery(p.Mediathek, new QueryMediathekCompleted([Item("Das Boot")], 1));
        var field = Assert.Single(query.Fields);
        Assert.Multiple(
            () => Assert.Equal(new[] { "title", "topic" }, field.Fields),
            () => Assert.Equal("Das Boot", field.Query),
            () => Assert.Equal(3600, query.DurationMin));

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

        var query = AnswerQuery(p.Mediathek, new QueryMediathekCompleted([], 0));
        Assert.Empty(query.Fields);

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

        var query = AnswerQuery(p.Mediathek, new QueryMediathekCompleted([Item("Tatort")], 1));
        Assert.Equal("Tatort", Assert.Single(query.Fields).Query);

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
    public void Evaluation_failure_returns_search_failed()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchMovie(searchId, SearchSource.Radarr, "Das Boot", null, null, null, null), TestActor);

        AnswerQuery(p.Mediathek, new QueryMediathekCompleted([Item("Das Boot")], 1));
        var evaluation = p.Evaluation.ExpectMsg<EvaluateMovies>();
        p.Evaluation.Reply(new EvaluateMoviesFailed(evaluation.SearchId, new Exception("Scoring error")));

        var result = ExpectMsg<SearchMovieFailed>();
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Contains("Scoring error", result.Cause.Message));
    }

    private static QueryMediathek AnswerQuery(Akka.TestKit.TestProbe mediathek, QueryMediathekResponse response,
        TimeSpan? timeout = null)
    {
        var query = mediathek.ExpectMsg<QueryMediathek>(timeout);
        mediathek.Reply(response);
        return query;
    }

    private static MediathekItem Item(string title) =>
        new("ARD", title, title, null, 1719244800, 7200, 0, null, "https://x.com/v.mp4", null, null, null);
}
