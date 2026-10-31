using Akka.Actor;
using Akka.Event;
using FunkArr.Messages.Feed;

namespace FunkArr.ArrApi.Newznab;

public sealed class FeedCacheActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public FeedCacheActor(FeedResultCache cache)
    {
        Receive<FeedChanged>(_ =>
        {
            cache.Clear();
            _log.Debug("Feed changed, cleared cached RSS pages");
        });
    }
}
