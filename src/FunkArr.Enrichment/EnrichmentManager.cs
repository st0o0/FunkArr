using Akka.Actor;
using Akka.Routing;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;

namespace FunkArr.Enrichment;

public sealed class EnrichmentManager : ReceiveActor
{
    public EnrichmentManager(TvdbClient tvdbClient, TmdbClient tmdbClient)
    {
        var tvdbPool = Context.ResolveChildActor<TvdbEnrichmentActor>("tvdb-pool",
            props => props.WithRouter(new SmallestMailboxPool(2)));

        var tmdbPool = Context.ResolveChildActor<TmdbEnrichmentActor>("tmdb-pool",
            props => props.WithRouter(new SmallestMailboxPool(2)));

        Receive<EnrichEpisodes>(msg => tvdbPool.Forward(msg));
        Receive<EnrichMovies>(msg => tmdbPool.Forward(msg));
        Receive<QueryCacheStats>(_ =>
            Sender.Tell(new CacheStatsResult(tvdbClient.CacheEntryCount, tmdbClient.CacheEntryCount, null)));
    }
}
