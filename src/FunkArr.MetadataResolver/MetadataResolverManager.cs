using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using FunkArr.Core;
using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

public sealed class MetadataResolverManager : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _tvdbPool;
    private readonly IActorRef _tmdbPool;
    private readonly Dictionary<int, EpisodeCacheEntry> _episodeCache = new();
    private readonly Dictionary<int, MovieCacheEntry> _movieCache = new();

    public MetadataResolverManager()
    {
        _tvdbPool = Context.ResolveChildActor<TvdbResolverActor>("tvdb-pool",
            props => props.WithRouter(new SmallestMailboxPool(2)));

        _tmdbPool = Context.ResolveChildActor<TmdbResolverActor>("tmdb-pool",
            props => props.WithRouter(new SmallestMailboxPool(2)));

        Receive<ResolveEpisodes>(HandleResolveEpisodes);
        Receive<ResolveMovie>(HandleResolveMovie);
        Receive<EpisodeCacheUpdate>(HandleEpisodeCacheUpdate);
        Receive<MovieCacheUpdate>(HandleMovieCacheUpdate);
        Receive<QueryCacheStats>(HandleCacheStats);
    }

    private void HandleResolveEpisodes(ResolveEpisodes msg)
    {
        if (msg.Config.Strategy == "none")
        {
            Sender.Tell(new EpisodesResolved([]));
            return;
        }

        if (_episodeCache.TryGetValue(msg.TvdbId, out var cached) && !cached.IsExpired)
        {
            _log.Debug("TVDB cache hit for series {TvdbId}", msg.TvdbId);
            Sender.Tell(new EpisodesResolved(cached.Resolved));
            return;
        }

        _log.Debug("TVDB cache miss for series {TvdbId}, fetching", msg.TvdbId);
        _tvdbPool.Tell(new FetchAndResolveEpisodes(msg.TvdbId, msg.Season, msg.Config, msg.Candidates), Sender);
    }

    private void HandleResolveMovie(ResolveMovie msg)
    {
        var tmdbId = msg.TmdbId;
        if (tmdbId is not null && _movieCache.TryGetValue(tmdbId.Value, out var cached) && !cached.IsExpired)
        {
            _log.Debug("TMDB cache hit for movie {TmdbId}", tmdbId);
            Sender.Tell(new MoviesResolved(cached.Resolved));
            return;
        }

        _log.Debug("TMDB cache miss for movie TmdbId={TmdbId} ImdbId={ImdbId}, fetching", msg.TmdbId, msg.ImdbId);
        _tmdbPool.Tell(new FetchAndResolveMovie(msg.ImdbId, msg.TmdbId, msg.Candidates), Sender);
    }

    private void HandleEpisodeCacheUpdate(EpisodeCacheUpdate msg)
    {
        var ttl = CacheTtl.DetermineShowTtl(msg.Episodes);
        _episodeCache[msg.TvdbId] = new EpisodeCacheEntry(msg.Episodes, msg.Resolved, DateTimeOffset.UtcNow, ttl);
    }

    private void HandleMovieCacheUpdate(MovieCacheUpdate msg)
    {
        _movieCache[msg.TmdbId] = new MovieCacheEntry(msg.Resolved, DateTimeOffset.UtcNow, CacheTtl.Movie);
    }

    private void HandleCacheStats(QueryCacheStats _)
    {
        var allEntries = _episodeCache.Values.Select(e => e.FetchedAt)
            .Concat(_movieCache.Values.Select(e => e.FetchedAt));
        var oldest = allEntries.Any() ? allEntries.Min() : (DateTimeOffset?)null;
        Sender.Tell(new CacheStatsResult(_episodeCache.Count, _movieCache.Count, oldest));
    }
}
