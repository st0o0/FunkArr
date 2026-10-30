using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class TvSearchWorkerTests : TestKit
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
        registry.Register<ITvEvaluationRegion>(probes.Evaluation);
        return probes;
    }

    private IActorRef CreateWorker(Guid searchId) =>
        Sys.ActorOf(Props.Create(() => new TvSearchWorker(searchId.ToString())));

    [Fact]
    public void Text_search_queries_mediathek_and_delegates_evaluation()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchSeries(searchId, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), TestActor);

        var query = AnswerQueries(p.Mediathek, new QueryMediathekCompleted([Item("Tatort", "Tatort: Test", "https://x.com/1.mp4")], 1));
        var field = Assert.Single(query.Fields);
        Assert.Multiple(
            () => Assert.Equal("topic", Assert.Single(field.Fields)),
            () => Assert.Equal("Tatort", field.Query),
            () => Assert.Equal(300, query.DurationMin),
            () => Assert.False(query.Future));
        p.Resolver.ExpectNoMsg(TimeSpan.FromMilliseconds(100));

        var evaluation = p.Evaluation.ExpectMsg<EvaluateSeries>();
        Assert.Multiple(
            () => Assert.NotEqual(searchId, evaluation.SearchId),
            () => Assert.Equal(SearchSource.Sonarr, evaluation.Source),
            () => Assert.Equal("Tatort: Test", Assert.Single(evaluation.Items).Title),
            () => Assert.Null(evaluation.RuleSet));
        var resultItem = new SearchResultItem("Tatort.S01E01.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Tatort",
            "https://x.com/1.mp4", 5400, 0, 720, null, 0.9);
        p.Evaluation.Reply(new EvaluateSeriesCompleted(evaluation.SearchId, [resultItem], 1));

        var result = ExpectMsg<SearchSeriesCompleted>();
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Equal(1, result.Total),
            () => Assert.Equal(resultItem, Assert.Single(result.Items)));
    }

    [Fact]
    public void Tvdb_id_search_resolves_ruleset_before_querying_and_passes_it_on()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchSeries(searchId, SearchSource.Sonarr, null, 2026, null, 83214, null, null, null), TestActor);

        var resolve = p.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Multiple(
            () => Assert.Null(resolve.TopicOrAlias),
            () => Assert.Equal(83214, resolve.Ids?.TvdbId));
        var ruleSet = new RuleSetResolved("tatort", "Tatort");
        p.Resolver.Reply(ruleSet);

        var query = AnswerQueries(p.Mediathek, new QueryMediathekCompleted([Item("Tatort", "Roomservice", "https://x.com/1.mp4")], 1));
        Assert.Equal("Tatort", Assert.Single(query.Fields).Query);

        var evaluation = p.Evaluation.ExpectMsg<EvaluateSeries>();
        Assert.Multiple(
            () => Assert.Equal(ruleSet, evaluation.RuleSet),
            () => Assert.Equal(83214, evaluation.TvdbId),
            () => Assert.Equal(2026, evaluation.Season));
    }

    [Fact]
    public void Tvdb_id_search_unresolved_returns_empty()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchSeries(searchId, SearchSource.Sonarr, null, null, null, 99999, null, null, null), TestActor);

        p.Resolver.ExpectMsg<ResolveRuleSet>();
        p.Resolver.Reply(new RuleSetFailed(new RuleSetNotFoundException("")));

        var result = ExpectMsg<SearchSeriesCompleted>();
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Empty(result.Items),
            () => Assert.Equal(0, result.Total));
        p.Mediathek.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Zero_mediathek_results_return_empty_without_evaluation()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchSeries(searchId, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), TestActor);

        AnswerQueries(p.Mediathek, new QueryMediathekCompleted([], 0));

        var result = ExpectMsg<SearchSeriesCompleted>();
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Empty(result.Items));
        p.Evaluation.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Include_future_sends_two_queries_and_merges_results_by_url()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchSeries(searchId, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), TestActor);

        var first = p.Mediathek.ExpectMsg<QueryMediathek>();
        var firstSender = p.Mediathek.LastSender;
        var second = p.Mediathek.ExpectMsg<QueryMediathek>();
        var secondSender = p.Mediathek.LastSender;
        Assert.Equal(new[] { false, true }, new[] { first.Future, second.Future }.Order().ToArray());

        var past = Item("Tatort", "Herz aus Eis", "https://x.com/past.mp4");
        var future = Item("Tatort", "Borowski", "https://x.com/future.mp4");
        firstSender.Tell(new QueryMediathekCompleted(first.Future ? [future, past] : [past], 2), p.Mediathek.Ref);
        secondSender.Tell(new QueryMediathekCompleted(second.Future ? [future, past] : [past], 2), p.Mediathek.Ref);

        var evaluation = p.Evaluation.ExpectMsg<EvaluateSeries>();
        Assert.Equal(2, evaluation.Items.Length);
        Assert.Equal(new[] { "https://x.com/future.mp4", "https://x.com/past.mp4" },
            evaluation.Items.Select(i => i.UrlVideo).Order().ToArray());
    }

    [Fact]
    public void Evaluation_failure_returns_search_failed()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchSeries(searchId, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), TestActor);

        AnswerQueries(p.Mediathek, new QueryMediathekCompleted([Item("Tatort", "Test", "https://x.com/v.mp4")], 1));
        var evaluation = p.Evaluation.ExpectMsg<EvaluateSeries>();
        p.Evaluation.Reply(new EvaluateSeriesFailed(evaluation.SearchId, new Exception("Scoring error")));

        var result = ExpectMsg<SearchSeriesFailed>();
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Contains("Scoring error", result.Cause.Message));
    }

    [Fact]
    public void Mediathek_error_is_not_retried()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchSeries(searchId, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), TestActor);

        AnswerQueries(p.Mediathek, new QueryMediathekError(new Exception("HTTP 500")));

        var result = ExpectMsg<SearchSeriesFailed>();
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Contains("HTTP 500", result.Cause.Message));
        p.Mediathek.ExpectNoMsg(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Queue_full_retries_and_succeeds()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchSeries(searchId, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), TestActor);

        AnswerQueries(p.Mediathek, new QueryMediathekQueueFull());

        AnswerQueries(p.Mediathek, new QueryMediathekCompleted([Item("Tatort", "Test", "https://x.com/v.mp4")], 1), TimeSpan.FromSeconds(5));

        var evaluation = p.Evaluation.ExpectMsg<EvaluateSeries>(TimeSpan.FromSeconds(5));
        p.Evaluation.Reply(new EvaluateSeriesCompleted(evaluation.SearchId, [], 0));

        var result = ExpectMsg<SearchSeriesCompleted>(TimeSpan.FromSeconds(5));
        Assert.Equal(searchId, result.SearchId);
    }

    [Fact]
    public void Queue_full_retries_exhausted_returns_failed()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = CreateWorker(searchId);

        worker.Tell(new SearchSeries(searchId, SearchSource.Sonarr, "Tatort", null, null, null, null, null, null), TestActor);

        AnswerQueries(p.Mediathek, new QueryMediathekQueueFull());
        AnswerQueries(p.Mediathek, new QueryMediathekQueueFull(), TimeSpan.FromSeconds(5));
        AnswerQueries(p.Mediathek, new QueryMediathekQueueFull(), TimeSpan.FromSeconds(5));

        var result = ExpectMsg<SearchSeriesFailed>(TimeSpan.FromSeconds(5));
        Assert.Multiple(
            () => Assert.Equal(searchId, result.SearchId),
            () => Assert.Contains("queue full", result.Cause.Message));
    }

    private static QueryMediathek AnswerQueries(Akka.TestKit.TestProbe mediathek, QueryMediathekResponse response,
        TimeSpan? timeout = null)
    {
        var queries = new List<QueryMediathek>();
        for (var i = 0; i < 2; i++)
        {
            queries.Add(mediathek.ExpectMsg<QueryMediathek>(timeout));
            mediathek.LastSender.Tell(response, mediathek.Ref);
        }

        Assert.Equal(new[] { false, true }, queries.Select(q => q.Future).Order().ToArray());
        return queries.Single(q => !q.Future);
    }

    private static MediathekItem Item(string topic, string title, string url) =>
        new("ARD", topic, title, null, 1719244800, 5400, 0, null, url, null, null, null);
}
