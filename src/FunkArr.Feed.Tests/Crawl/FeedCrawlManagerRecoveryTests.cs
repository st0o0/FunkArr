using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using Akka.TestKit;
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

public sealed class FeedCrawlManagerRecoveryTests : TestKit
{
    private static readonly Config _persistenceConfig = ConfigurationFactory.ParseString("""
        akka.persistence {
            journal {
                plugin = "akka.persistence.journal.inmem"
                inmem {
                    class = "Akka.Persistence.Journal.MemoryJournal, Akka.Persistence"
                    plugin-dispatcher = "akka.actor.default-dispatcher"
                }
            }
            snapshot-store {
                plugin = "akka.persistence.snapshot-store.inmem"
                inmem {
                    class = "Akka.Persistence.Snapshot.MemorySnapshotStore, Akka.Persistence"
                    plugin-dispatcher = "akka.actor.default-dispatcher"
                }
            }
        }
        """);

    public FeedCrawlManagerRecoveryTests() : base(_persistenceConfig) { }

    private const long ListTimestamp = 1_761_900_000;
    private static readonly TimeSpan _stageTimeout = TimeSpan.FromSeconds(5);

    private static readonly RegisteredRuleSetEntry _tatort =
        new("tatort", "Tatort", [], new ExternalIds(76290, "tt0806910", null), "Tatort", MediaType.Show);

    private sealed record Probes(
        TestProbe Mediathek,
        TestProbe Resolver,
        TestProbe Tv,
        TestProbe Movie,
        TestProbe Store);

    private (IActorRef Manager, Probes Probes) Create(FeedOptions? feedOptions = null)
    {
        var probes = new Probes(CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe(), CreateTestProbe());
        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(probes.Mediathek, overwrite: true);
        registry.Register<IRuleSetResolver>(probes.Resolver, overwrite: true);
        registry.Register<ITvEvaluationRegion>(probes.Tv, overwrite: true);
        registry.Register<IMovieEvaluationRegion>(probes.Movie, overwrite: true);
        registry.Register<IFeedStoreManager>(probes.Store, overwrite: true);

        var options = Substitute.For<IOptionsMonitor<FeedOptions>>();
        options.CurrentValue.Returns(feedOptions ?? new FeedOptions { PageSize = 2, MaxPages = 2 });

        var manager = Sys.ActorOf(Props.Create(() => new FeedCrawlManager(options, TimeProvider.System)));
        registry.Register<IFeedCrawlManager>(manager, overwrite: true);
        return (manager, probes);
    }

    private static void AnswerProbe(Probes p, long listTimestamp = ListTimestamp)
    {
        var probe = p.Mediathek.ExpectMsg<QueryMediathek>();
        p.Mediathek.Reply(new QueryMediathekCompleted([], 0, listTimestamp));
    }

    private static void AnswerRuleSets(Probes p)
    {
        p.Resolver.ExpectMsg<QueryRegisteredRuleSets>();
        p.Resolver.Reply(new RegisteredRuleSetsResult([_tatort]));
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

    private void DriveCompletedCrawl(IActorRef manager, Probes p)
    {
        var tatort = TestData.Item("Herz aus Eis", "Tatort", ListTimestamp, url: "https://example.com/tatort.mp4");

        manager.Tell(new StartFeedCrawl());
        AnswerProbe(p);
        AnswerRuleSets(p);

        var pageQuery = p.Mediathek.ExpectMsg<QueryMediathek>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        p.Mediathek.Reply(new QueryMediathekCompleted([tatort], 1, ListTimestamp));

        var eval = p.Tv.ExpectMsg<EvaluateSeries>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        p.Tv.Reply(new EvaluateSeriesCompleted(eval.SearchId, [TestData.Result("https://example.com/tatort.mp4")], 1));

        var publish = p.Store.ExpectMsg<PublishFeedEntries>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        p.Store.Reply(new PublishFeedEntriesCompleted(1));

        AwaitStatus(manager, s => s.LastOutcome == CrawlOutcome.Completed);
    }

    [Fact]
    public void Cursor_survives_restart()
    {
        var (manager, probes) = Create();
        DriveCompletedCrawl(manager, probes);

        var preStatus = AwaitStatus(manager, s => s.Cursor == ListTimestamp);
        Assert.Equal(ListTimestamp, preStatus.Cursor);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var (manager2, _) = Create();
        var postStatus = AwaitStatus(manager2, s => s.Cursor == ListTimestamp);

        Assert.Equal(ListTimestamp, postStatus.Cursor);
        Assert.Equal(CrawlOutcome.Completed, postStatus.LastOutcome);
    }

    [Fact]
    public void ConsecutiveFailures_survives_restart()
    {
        var (manager, probes) = Create();

        manager.Tell(new StartFeedCrawl());
        AnswerProbe(probes);
        AnswerRuleSets(probes);

        var pageQuery = probes.Mediathek.ExpectMsg<QueryMediathek>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        probes.Mediathek.Reply(new QueryMediathekError(new Exception("Server error")));

        AwaitStatus(manager, s => s.ConsecutiveFailures >= 1);

        manager.Tell(new StartFeedCrawl());
        AnswerProbe(probes);
        AnswerRuleSets(probes);

        pageQuery = probes.Mediathek.ExpectMsg<QueryMediathek>(_stageTimeout, cancellationToken: TestContext.Current.CancellationToken);
        probes.Mediathek.Reply(new QueryMediathekError(new Exception("Server error again")));

        AwaitStatus(manager, s => s.ConsecutiveFailures >= 2);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var (manager2, _) = Create();
        var postStatus = AwaitStatus(manager2, s => s.ConsecutiveFailures >= 2);

        Assert.Equal(2, postStatus.ConsecutiveFailures);
    }

    [Fact]
    public void Unchanged_run_persisted_and_survives_restart()
    {
        var (manager, probes) = Create();

        manager.Tell(new StartFeedCrawl());
        AnswerProbe(probes, listTimestamp: 0);

        AwaitStatus(manager, s => s.LastOutcome == CrawlOutcome.Unchanged);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var (manager2, _) = Create();
        var postStatus = AwaitStatus(manager2, s => s.LastOutcome == CrawlOutcome.Unchanged);

        Assert.Equal(CrawlOutcome.Unchanged, postStatus.LastOutcome);
    }
}
