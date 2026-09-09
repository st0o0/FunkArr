using Akka.Actor;
using Akka.Routing;
using FunkArr.Core;
using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

public sealed class MetadataResolverManager : ReceiveActor
{
    public MetadataResolverManager(TvdbClient tvdbClient, TmdbClient tmdbClient)
    {
        var tvdbPool = Context.ResolveChildActor<TvdbResolverActor>("tvdb-pool",
            props => props.WithRouter(new SmallestMailboxPool(2)));

        var tmdbPool = Context.ResolveChildActor<TmdbResolverActor>("tmdb-pool",
            props => props.WithRouter(new SmallestMailboxPool(2)));

        Receive<ResolveEpisodes>(msg => tvdbPool.Forward(msg));
        Receive<ResolveMovie>(msg => tmdbPool.Forward(msg));
        Receive<QueryCacheStats>(_ =>
            Sender.Tell(new CacheStatsResult(tvdbClient.CacheEntryCount, tmdbClient.CacheEntryCount, null)));
    }
}
