using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class EvaluateWorkerTests : TestKit
{
    private sealed record TestProbes(
        Akka.TestKit.TestProbe Mediathek,
        Akka.TestKit.TestProbe Scoring,
        Akka.TestKit.TestProbe Resolver,
        Akka.TestKit.TestProbe Enrichment,
        Akka.TestKit.TestProbe History);

    private TestProbes RegisterProbes()
    {
        var probes = new TestProbes(CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe());
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(probes.Mediathek);
        registry.Register<IScoringManager>(probes.Scoring);
        registry.Register<IRuleSetResolver>(probes.Resolver);
        registry.Register<IEnrichmentManager>(probes.Enrichment);
        registry.Register<IHistoryRegion>(probes.History);
        return probes;
    }

    [Fact]
    public void EvaluateSeries_resolves_ruleset_from_items_without_querying_mediathek()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker(searchId.ToString(), null, null)));

        worker.Tell(new EvaluateSeries(searchId, SearchSource.Feed, [Item("Tatort", "Herz aus Eis")]), TestActor);

        var resolve = p.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Equal("Tatort", resolve.TopicOrAlias);
        p.Resolver.Reply(new RuleSetResolved("tatort", "Tatort", "Tatort"));

        var score = p.Scoring.ExpectMsg<ScoreItems>();
        Assert.Multiple(
            () => Assert.Equal("tatort", score.RuleSetId),
            () => Assert.Equal(SearchSource.Feed, score.Origin.Source),
            () => Assert.Single(score.Candidates));
        p.Mediathek.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void EvaluateSeries_with_no_items_replies_empty()
    {
        RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker(searchId.ToString(), null, null)));

        worker.Tell(new EvaluateSeries(searchId, SearchSource.Feed, []), TestActor);

        var completed = ExpectMsg<SearchSeriesCompleted>();
        Assert.Empty(completed.Items);
    }

    [Fact]
    public void EvaluateMovies_resolves_ruleset_from_items_without_querying_mediathek()
    {
        var p = RegisterProbes();
        var searchId = Guid.NewGuid();
        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker(searchId.ToString(), null, null)));

        worker.Tell(new EvaluateMovies(searchId, SearchSource.Feed, [Item("Das Boot", "Das Boot")]), TestActor);

        var resolve = p.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Equal("Das Boot", resolve.TopicOrAlias);
        p.Mediathek.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
    }

    private static MediathekItem Item(string topic, string title) =>
        new("ARD", topic, title, null, 1761800000, 5400, 0, null, "https://example.com/v.mp4", null, null, null,
            FilmlisteTimestamp: 1761800000);
}
