using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Feed.Crawl;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.Feed.Tests.Crawl;

public sealed class FeedHarvestActorTests : TestKit
{
    private const long Cursor = 1_761_800_000;
    private const long ListTimestamp = 1_761_900_000;
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    private static readonly RegisteredRuleSetEntry _tatort =
        new("tatort", "Tatort", [], new ExternalIds(76290, "tt0806910", null), "Tatort", MediaType.Show);

    private static readonly FeedOptions _defaultOptions = new() { PageSize = 10, MaxPages = 2 };

    private sealed record Probes(
        Akka.TestKit.TestProbe Mediathek,
        Akka.TestKit.TestProbe Resolver,
        Akka.TestKit.TestProbe Tv,
        Akka.TestKit.TestProbe Movie,
        Akka.TestKit.TestProbe Store);

    private (IActorRef Harvester, Probes Probes) Create()
    {
        var probes = new Probes(
            CreateTestProbe(), CreateTestProbe(), CreateTestProbe(),
            CreateTestProbe(), CreateTestProbe());
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(probes.Mediathek);
        registry.Register<IRuleSetResolver>(probes.Resolver);
        registry.Register<ITvEvaluationRegion>(probes.Tv);
        registry.Register<IMovieEvaluationRegion>(probes.Movie);
        registry.Register<IFeedStoreManager>(probes.Store);

        var harvester = Sys.ActorOf(
            Props.Create(() => new FeedHarvestActor(TimeProvider.System)));
        return (harvester, probes);
    }

    [Fact]
    public void Unchanged_returns_HarvestUnchanged()
    {
        var (harvester, p) = Create();

        harvester.Tell(new StartHarvest(Cursor, _defaultOptions));

        var probe = p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, probe.Size);
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, Cursor));

        ExpectMsg<HarvestUnchanged>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Zero_timestamp_returns_HarvestUnchanged()
    {
        var (harvester, p) = Create();

        harvester.Tell(new StartHarvest(Cursor, _defaultOptions));

        p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, 0));

        ExpectMsg<HarvestUnchanged>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Full_pipeline_fetches_evaluates_publishes()
    {
        var (harvester, p) = Create();
        var tatort = TestData.Item("Herz aus Eis", "Tatort", ListTimestamp,
            url: "https://example.com/tatort.mp4");

        harvester.Tell(new StartHarvest(Cursor, _defaultOptions));

        p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, ListTimestamp));

        p.Resolver.ExpectMsg<QueryRegisteredRuleSets>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Resolver.Reply(new RegisteredRuleSetsResult([_tatort]));

        var pageQuery = p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, pageQuery.Offset);
        p.Mediathek.Reply(new QueryMediathekCompleted([tatort], 1, ListTimestamp));

        var eval = p.Tv.ExpectMsg<EvaluateSeries>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Multiple(
            () => Assert.Equal(SearchSource.Feed, eval.Source),
            () => Assert.Equal(76290, eval.TvdbId));
        p.Tv.Reply(new EvaluateSeriesCompleted(eval.SearchId,
            [TestData.Result("https://example.com/tatort.mp4")], 1));

        var publish = p.Store.ExpectMsg<PublishFeedEntries>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(publish.Entries);
        p.Store.Reply(new PublishFeedEntriesCompleted(1));

        var result = ExpectMsg<HarvestCompleted>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, result.Summary.Status),
            () => Assert.Equal(ListTimestamp, result.Summary.ListTimestamp),
            () => Assert.Equal(1, result.Summary.Accepted));
    }

    [Fact]
    public void No_matching_topics_completes_with_zero_published()
    {
        var (harvester, p) = Create();
        var unknown = TestData.Item("Irgendwas", "UnbekanntesSendung", ListTimestamp);

        harvester.Tell(new StartHarvest(Cursor, _defaultOptions));

        p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, ListTimestamp));

        p.Resolver.ExpectMsg<QueryRegisteredRuleSets>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Resolver.Reply(new RegisteredRuleSetsResult([_tatort]));

        p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Mediathek.Reply(new QueryMediathekCompleted([unknown], 1, ListTimestamp));

        var result = ExpectMsg<HarvestCompleted>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, result.Summary.Status),
            () => Assert.Equal(0, result.Summary.Accepted));
    }

    [Fact]
    public void Page_fetch_error_response_completes_with_zero_entries()
    {
        var (harvester, p) = Create();

        harvester.Tell(new StartHarvest(Cursor, _defaultOptions));

        p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, ListTimestamp));

        p.Resolver.ExpectMsg<QueryRegisteredRuleSets>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Resolver.Reply(new RegisteredRuleSetsResult([_tatort]));

        p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Mediathek.Reply(new QueryMediathekError(new InvalidOperationException("HTTP 500")));

        var result = ExpectMsg<HarvestCompleted>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, result.Summary.Status),
            () => Assert.Equal(0, result.Summary.Accepted));
    }

    [Fact]
    public void Filters_items_outside_window()
    {
        var (harvester, p) = Create();
        var inWindow = TestData.Item("InWindow", "Tatort", ListTimestamp,
            url: "https://example.com/in.mp4");
        var belowCursor = TestData.Item("BelowCursor", "Tatort", Cursor - 100,
            url: "https://example.com/below.mp4");

        harvester.Tell(new StartHarvest(Cursor, _defaultOptions));

        p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, ListTimestamp));

        p.Resolver.ExpectMsg<QueryRegisteredRuleSets>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Resolver.Reply(new RegisteredRuleSetsResult([_tatort]));

        p.Mediathek.ExpectMsg<QueryMediathek>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Mediathek.Reply(new QueryMediathekCompleted([inWindow, belowCursor], 2, ListTimestamp));

        var eval = p.Tv.ExpectMsg<EvaluateSeries>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("InWindow", Assert.Single(eval.Items).Title);
        p.Tv.Reply(new EvaluateSeriesCompleted(eval.SearchId,
            [TestData.Result("https://example.com/in.mp4")], 1));

        p.Store.ExpectMsg<PublishFeedEntries>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
        p.Store.Reply(new PublishFeedEntriesCompleted(1));

        ExpectMsg<HarvestCompleted>(_timeout,
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
