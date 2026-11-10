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
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FunkArr.Feed.Tests.Crawl;

public sealed class FeedCrawlManagerTests : TestKit
{
    private const long ListTimestamp = 1_761_900_000;
    private static readonly TimeSpan _stageTimeout = TimeSpan.FromSeconds(5);

    private static readonly RegisteredRuleSetEntry _tatort =
        new("tatort", "Tatort", [], new ExternalIds(76290, "tt0806910", null), "Tatort", MediaType.Show);

    private sealed record Probes(
        Akka.TestKit.TestProbe Mediathek,
        Akka.TestKit.TestProbe Resolver,
        Akka.TestKit.TestProbe Tv,
        Akka.TestKit.TestProbe Movie,
        Akka.TestKit.TestProbe Store);

    private (IActorRef Manager, Probes Probes) Create(FeedOptions? feedOptions = null)
    {
        var probes = new Probes(CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe());
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(probes.Mediathek);
        registry.Register<IRuleSetResolver>(probes.Resolver);
        registry.Register<ITvEvaluationRegion>(probes.Tv);
        registry.Register<IMovieEvaluationRegion>(probes.Movie);
        registry.Register<IFeedStoreManager>(probes.Store);

        var options = Substitute.For<IOptionsMonitor<FeedOptions>>();
        options.CurrentValue.Returns(feedOptions ?? new FeedOptions { PageSize = 2, MaxPages = 2, MaxConcurrency = 2 });

        var manager = Sys.ActorOf(Props.Create(() => new FeedCrawlManager(options, TimeProvider.System)));
        registry.Register<IFeedCrawlManager>(manager);
        return (manager, probes);
    }

    private static void AnswerProbe(Probes p, long listTimestamp = ListTimestamp)
    {
        var probe = p.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Multiple(
            () => Assert.Equal(1, probe.Size),
            () => Assert.Equal("filmlisteTimestamp", probe.SortBy),
            () => Assert.True(probe.Future));
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, listTimestamp));
    }

    private static void AnswerRuleSets(Probes p)
    {
        p.Resolver.ExpectMsg<QueryRegisteredRuleSets>();
        p.Resolver.Reply(new RegisteredRuleSetsResult([_tatort]));
    }

    private static void AnswerPages(Probes p, Func<int, QueryMediathekResponse> page)
    {
        var query = p.Mediathek.ExpectMsg<QueryMediathek>(_stageTimeout);
        p.Mediathek.LastSender.Tell(page(query.Offset));

        // Second page may or may not arrive depending on stream scheduling
        if (p.Mediathek.ReceiveOne(TimeSpan.FromSeconds(1)) is QueryMediathek q2)
        {
            p.Mediathek.LastSender.Tell(page(q2.Offset));
        }
    }

    private FeedCrawlStatusResult AwaitStatus(IActorRef manager, Func<FeedCrawlStatusResult, bool> done)
    {
        FeedCrawlStatusResult? status = null;
        AwaitAssert(() =>
        {
            manager.Tell(new QueryFeedCrawlStatus());
            status = ExpectMsg<FeedCrawlStatusResult>();
            Assert.True(done(status));
        }, _stageTimeout);
        Assert.NotNull(status);
        return status;
    }

    [Fact]
    public void Unchanged_filmliste_costs_one_query()
    {
        var (manager, p) = Create();

        manager.Tell(new StartFeedCrawl());
        AnswerProbe(p, listTimestamp: 0);

        var status = AwaitStatus(manager, s => s.LastOutcome != CrawlOutcome.Unknown);
        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Unchanged, status.LastOutcome),
            () => Assert.False(status.Running),
            () => Assert.Equal(0, status.Cursor));
        p.Resolver.ExpectNoMsg(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Crawl_publishes_matched_entries_and_advances_the_cursor()
    {
        var (manager, p) = Create();
        var tatort = TestData.Item("Herz aus Eis", "Tatort", ListTimestamp, url: "https://example.com/tatort.mp4");
        var unknown = TestData.Item("Irgendwas", "Unbekannt", ListTimestamp - 10);
        var old = TestData.Item("Alt", "Tatort", ListTimestamp - 400_000);

        manager.Tell(new StartFeedCrawl());
        AnswerProbe(p);
        AnswerRuleSets(p);
        AnswerPages(p, offset => new QueryMediathekCompleted(offset == 0 ? [tatort, unknown] : [old], 3, ListTimestamp));

        var evaluation = p.Tv.ExpectMsg<EvaluateSeries>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Multiple(
            () => Assert.Equal(SearchSource.Feed, evaluation.Source),
            () => Assert.Equal(76290, evaluation.TvdbId),
            () => Assert.Equal("Herz aus Eis", Assert.Single(evaluation.Items).Title));
        p.Tv.Reply(new EvaluateSeriesCompleted(evaluation.SearchId, [TestData.Result("https://example.com/tatort.mp4")], 1));

        var publish = p.Store.ExpectMsg<PublishFeedEntries>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(ListTimestamp), Assert.Single(publish.Entries).FirstSeenAt);
        p.Store.Reply(new PublishFeedEntriesCompleted(1));

        var status = AwaitStatus(manager, s => s.LastOutcome != CrawlOutcome.Unknown);
        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, status.LastOutcome),
            () => Assert.Equal(ListTimestamp, status.Cursor),
            () => Assert.Equal(2, status.LastNewItems),
            () => Assert.Equal(1, status.LastPublished),
            () => Assert.Equal(1, status.LastUnmatched),
            () => Assert.False(status.LastGap),
            () => Assert.False(status.Running));
    }

    [Fact]
    public void Failed_eval_completes_crawl_with_zero_published()
    {
        var (manager, p) = Create();
        var tatort = TestData.Item("Herz aus Eis", "Tatort", ListTimestamp);
        var old = TestData.Item("Alt", "Tatort", ListTimestamp - 400_000);

        manager.Tell(new StartFeedCrawl());
        AnswerProbe(p);
        AnswerRuleSets(p);
        AnswerPages(p, offset => new QueryMediathekCompleted(offset == 0 ? [tatort] : [old], 2, ListTimestamp));
        var evaluation = p.Tv.ExpectMsg<EvaluateSeries>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        p.Tv.Reply(new EvaluateSeriesFailed(evaluation.SearchId, new InvalidOperationException("boom")));

        var status = AwaitStatus(manager, s => s.LastOutcome != CrawlOutcome.Unknown);
        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, status.LastOutcome),
            () => Assert.Equal(ListTimestamp, status.Cursor),
            () => Assert.Equal(0, status.LastPublished),
            () => Assert.False(status.Running));
        p.Store.ExpectNoMsg(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Failed_page_query_fails_the_crawl()
    {
        var (manager, p) = Create();

        manager.Tell(new StartFeedCrawl());
        AnswerProbe(p);
        AnswerRuleSets(p);

        for (var i = 0; i < 2; i++)
        {
            if (p.Mediathek.ReceiveOne(_stageTimeout, TestContext.Current.CancellationToken) is QueryMediathek)
            {
                p.Mediathek.LastSender.Tell(new QueryMediathekError(new InvalidOperationException("HTTP 500")));
            }
        }

        var status = AwaitStatus(manager, s => s.LastOutcome != CrawlOutcome.Unknown);
        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Failed, status.LastOutcome),
            () => Assert.Contains("HTTP 500", status.LastError!),
            () => Assert.Equal(0, status.Cursor));
    }

    [Fact]
    public void Second_start_while_running_is_ignored()
    {
        var (manager, p) = Create();

        manager.Tell(new StartFeedCrawl());
        p.Mediathek.ExpectMsg<QueryMediathek>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Tell(new StartFeedCrawl());

        p.Mediathek.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
        manager.Tell(new QueryFeedCrawlStatus());
        Assert.True(ExpectMsg<FeedCrawlStatusResult>(cancellationToken: TestContext.Current.CancellationToken).Running);
    }

    [Fact]
    public void Stashed_page_is_processed_after_current_page_completes()
    {
        var (manager, p) = Create();
        var tatort = TestData.Item("Herz aus Eis", "Tatort", ListTimestamp, url: "https://example.com/tatort1.mp4");
        var old = TestData.Item("Alt", "Tatort", ListTimestamp - 400_000);

        manager.Tell(new StartFeedCrawl());
        AnswerProbe(p);
        AnswerRuleSets(p);
        AnswerPages(p, offset => new QueryMediathekCompleted(offset == 0 ? [tatort, old] : [old], 2, ListTimestamp));

        var eval1 = p.Tv.ExpectMsg<EvaluateSeries>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        p.Tv.Reply(new EvaluateSeriesCompleted(eval1.SearchId, [TestData.Result("https://example.com/tatort1.mp4")], 1));

        var publish = p.Store.ExpectMsg<PublishFeedEntries>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(publish.Entries);
        p.Store.Reply(new PublishFeedEntriesCompleted(1));

        var status = AwaitStatus(manager, s => s.LastOutcome != CrawlOutcome.Unknown);
        Assert.Multiple(
            () => Assert.Equal(CrawlOutcome.Completed, status.LastOutcome),
            () => Assert.False(status.Running));
    }
}
