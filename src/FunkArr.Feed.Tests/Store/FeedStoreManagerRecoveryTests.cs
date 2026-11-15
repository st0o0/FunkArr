using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit.Xunit;
using FunkArr.Core.Configuration;
using FunkArr.Feed.Store;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FunkArr.Feed.Tests.Store;

public sealed class FeedStoreManagerRecoveryTests : TestKit
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

    public FeedStoreManagerRecoveryTests() : base(_persistenceConfig) { }

    private static readonly DateTimeOffset _firstSeen = DateTimeOffset.UtcNow;

    private IActorRef CreateManager(TimeSpan? retention = null)
    {
        var options = Substitute.For<IOptionsMonitor<FeedOptions>>();
        options.CurrentValue.Returns(new FeedOptions
        {
            Retention = retention ?? TimeSpan.FromDays(14)
        });
        return Sys.ActorOf(Props.Create(() => new FeedStoreManager(options, TimeProvider.System)));
    }

    [Fact]
    public void Entries_survive_restart()
    {
        var manager = CreateManager();

        manager.Tell(new PublishFeedEntries([
            TestData.Entry("a", _firstSeen),
            TestData.Entry("b", _firstSeen.AddMinutes(1)),
            TestData.Entry("c", _firstSeen.AddMinutes(2))
        ]));
        ExpectMsg<PublishFeedEntriesCompleted>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new QueryFeed(null, 0, 10));
        var result = ExpectMsg<FeedResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, result.Total);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager();
        manager2.Tell(new QueryFeed(null, 0, 10));
        var result2 = ExpectMsg<FeedResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, result2.Total);
        Assert.Contains(result2.Entries, e => e.StableId == "a");
        Assert.Contains(result2.Entries, e => e.StableId == "b");
        Assert.Contains(result2.Entries, e => e.StableId == "c");
    }

    [Fact]
    public void Expired_entries_stay_expired_after_restart()
    {
        var manager = CreateManager(retention: TimeSpan.FromMilliseconds(1));

        manager.Tell(new PublishFeedEntries([
            TestData.Entry("old", DateTimeOffset.UtcNow.AddDays(-1))
        ]));
        ExpectMsg<PublishFeedEntriesCompleted>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new PublishFeedEntries([
            TestData.Entry("new", DateTimeOffset.UtcNow)
        ]));
        ExpectMsg<PublishFeedEntriesCompleted>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new QueryFeed(null, 0, 10));
        var result = ExpectMsg<FeedResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, result.Total);
        Assert.Equal("new", result.Entries[0].StableId);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager(retention: TimeSpan.FromMilliseconds(1));
        manager2.Tell(new QueryFeed(null, 0, 10));
        var result2 = ExpectMsg<FeedResult>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result2.Total);
        Assert.Equal("new", result2.Entries[0].StableId);
    }

    [Fact]
    public void Deduplication_works_after_restart()
    {
        var manager = CreateManager();

        manager.Tell(new PublishFeedEntries([TestData.Entry("a", _firstSeen)]));
        ExpectMsg<PublishFeedEntriesCompleted>(cancellationToken: TestContext.Current.CancellationToken);

        Watch(manager);
        Sys.Stop(manager);
        ExpectTerminated(manager, cancellationToken: TestContext.Current.CancellationToken);

        var manager2 = CreateManager();
        manager2.Tell(new PublishFeedEntries([TestData.Entry("a", _firstSeen)]));
        var result = ExpectMsg<PublishFeedEntriesCompleted>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, result.Published);
    }
}
