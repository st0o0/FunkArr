using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Core.Configuration;
using FunkArr.Feed.Store;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FunkArr.Feed.Tests.Store;

public sealed class FeedStoreManagerTests : TestKit
{
    private static readonly DateTimeOffset _firstSeen = DateTimeOffset.UtcNow;

    private IActorRef CreateManager()
    {
        var options = Substitute.For<IOptionsMonitor<FeedOptions>>();
        options.CurrentValue.Returns(new FeedOptions());
        return Sys.ActorOf(Props.Create(() => new FeedStoreManager(options, TimeProvider.System)));
    }

    [Fact]
    public void Adding_new_entries_persists_and_returns_count()
    {
        var manager = CreateManager();

        manager.Tell(new PublishFeedEntries([TestData.Entry("a", _firstSeen)]));

        var completed = ExpectMsg<PublishFeedEntriesCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, completed.Published);
    }

    [Fact]
    public void Adding_only_known_entries_returns_zero()
    {
        var manager = CreateManager();
        manager.Tell(new PublishFeedEntries([TestData.Entry("a", _firstSeen)]));
        ExpectMsg<PublishFeedEntriesCompleted>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new PublishFeedEntries([TestData.Entry("a", _firstSeen)]));

        var completed = ExpectMsg<PublishFeedEntriesCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, completed.Published);
    }

    [Fact]
    public void Query_returns_entries_after_add()
    {
        var manager = CreateManager();
        manager.Tell(new PublishFeedEntries([TestData.Entry("a", _firstSeen)]));
        ExpectMsg<PublishFeedEntriesCompleted>(cancellationToken: TestContext.Current.CancellationToken);

        manager.Tell(new QueryFeed(MediaType.Show, 0, 10));

        var result = ExpectMsg<FeedResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("a", Assert.Single(result.Entries).StableId);
    }
}
