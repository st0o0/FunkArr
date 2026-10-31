using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FunkArr.Feed.Tests;

public sealed class FeedManagerTests : TestKit
{
    private static readonly DateTimeOffset _firstSeen = DateTimeOffset.UtcNow;

    private readonly IFeedCacheInvalidator _invalidator = Substitute.For<IFeedCacheInvalidator>();

    private IActorRef CreateManager()
    {
        _invalidator.InvalidateAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var options = Substitute.For<IOptionsMonitor<FeedOptions>>();
        options.CurrentValue.Returns(new FeedOptions());
        var invalidator = _invalidator;
        return Sys.ActorOf(Props.Create(() => new FeedManager(options, invalidator, TimeProvider.System)));
    }

    [Fact]
    public void Recovery_invalidates_the_feed_cache_once()
    {
        CreateManager();

        AwaitAssert(() => _invalidator.Received(1).InvalidateAsync(Arg.Any<CancellationToken>()), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Adding_new_entries_invalidates_the_feed_cache()
    {
        var manager = CreateManager();
        AwaitAssert(() => _invalidator.Received(1).InvalidateAsync(Arg.Any<CancellationToken>()), TimeSpan.FromSeconds(3));

        manager.Tell(new AddFeedEntries([TestData.Entry("a", _firstSeen)]));

        var completed = ExpectMsg<AddFeedEntriesCompleted>();
        Assert.Equal(1, completed.Added);
        AwaitAssert(() => _invalidator.Received(2).InvalidateAsync(Arg.Any<CancellationToken>()), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Adding_only_known_entries_keeps_the_feed_cache()
    {
        var manager = CreateManager();
        manager.Tell(new AddFeedEntries([TestData.Entry("a", _firstSeen)]));
        ExpectMsg<AddFeedEntriesCompleted>();
        AwaitAssert(() => _invalidator.Received(2).InvalidateAsync(Arg.Any<CancellationToken>()), TimeSpan.FromSeconds(3));

        manager.Tell(new AddFeedEntries([TestData.Entry("a", _firstSeen)]));

        var completed = ExpectMsg<AddFeedEntriesCompleted>();
        Assert.Equal(0, completed.Added);
        _invalidator.Received(2).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Failed_invalidation_does_not_affect_the_feed()
    {
        var manager = CreateManager();
        _invalidator.InvalidateAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("store down")));

        manager.Tell(new AddFeedEntries([TestData.Entry("a", _firstSeen)]));
        ExpectMsg<AddFeedEntriesCompleted>();
        manager.Tell(new QueryFeed(MediaType.Show, 0, 10));

        var result = ExpectMsg<FeedResult>();
        Assert.Equal("a", Assert.Single(result.Entries).StableId);
    }
}
