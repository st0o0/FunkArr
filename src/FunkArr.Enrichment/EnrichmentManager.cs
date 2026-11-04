using Akka.Actor;
using Akka.Routing;
using FunkArr.Enrichment.Tmdb;
using FunkArr.Enrichment.Tvdb;
using FunkArr.Messages.Enrichment;
using Terp.Akka;

namespace FunkArr.Enrichment;

public sealed class EnrichmentManager : ReceiveActor
{
    public EnrichmentManager()
    {
        var tvdbPool = Context.ResolveChildActor<TvdbEnrichmentActor>("tvdb-pool",
            props => props.WithRouter(new SmallestMailboxPool(2)));

        var tmdbPool = Context.ResolveChildActor<TmdbEnrichmentActor>("tmdb-pool",
            props => props.WithRouter(new SmallestMailboxPool(2)));

        Receive<EnrichEpisodes>(tvdbPool.Forward);
        Receive<EnrichMovies>(tmdbPool.Forward);
    }
}
