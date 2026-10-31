using Akka.Actor;
using Akka.TestKit.Xunit;
using FunkArr.ArrApi.Newznab;
using FunkArr.Messages;
using FunkArr.Messages.Feed;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class FeedCacheActorTests : TestKit
{
    [Fact]
    public void Feed_changed_clears_the_cached_pages()
    {
        var cache = new FeedResultCache();
        var key = new FeedPageKey(MediaType.Show, 0, 100);
        cache.Store(key, new FeedResult([], 1), cache.Generation);
        var actor = Sys.ActorOf(Props.Create(() => new FeedCacheActor(cache)));

        actor.Tell(new FeedChanged());

        AwaitAssert(() => Assert.False(cache.TryGet(key, out _)), TimeSpan.FromSeconds(3));
    }
}
