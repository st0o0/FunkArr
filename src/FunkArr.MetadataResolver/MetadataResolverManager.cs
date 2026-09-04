using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using FunkArr.Messages.MetadataResolver;

namespace FunkArr.MetadataResolver;

public sealed class MetadataResolverManager : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly TvdbClient _tvdbClient;
    private readonly TmdbClient _tmdbClient;
    private readonly IActorRef _tvdbPool;
    private readonly IActorRef _tmdbPool;
    private readonly Dictionary<(string Provider, int Id), CacheEntry> _cache = new();

    public MetadataResolverManager(TvdbClient tvdbClient, TmdbClient tmdbClient)
    {
        _tvdbClient = tvdbClient;
        _tmdbClient = tmdbClient;

        _tvdbPool = Context.ActorOf(
            Props.Create(() => new TvdbResolverActor(tvdbClient))
                .WithRouter(new SmallestMailboxPool(2)),
            "tvdb-pool");

        _tmdbPool = Context.ActorOf(
            Props.Create(() => new TmdbResolverActor(tmdbClient))
                .WithRouter(new SmallestMailboxPool(2)),
            "tmdb-pool");

        Receive<ResolveEpisodes>(HandleResolveEpisodes);
        Receive<ResolveMovie>(HandleResolveMovie);
        Receive<CacheUpdate>(HandleCacheUpdate);
        Receive<QueryCacheStats>(HandleCacheStats);
    }

    private void HandleResolveEpisodes(ResolveEpisodes msg)
    {
        if (msg.Config.Strategy == "none")
        {
            Sender.Tell(new EpisodesResolved([]));
            return;
        }

        if (!_tvdbClient.IsConfigured)
        {
            Sender.Tell(new EpisodeResolutionFailed("TVDB API key is not configured"));
            return;
        }

        if (_cache.TryGetValue(("tvdb", msg.TvdbId), out var cached) && !cached.IsExpired)
        {
            var episodes = (TvdbEpisode[])cached.Data;
            var filtered = FilterBySeason(episodes, msg.Season);
            var resolved = EpisodeResolver.Resolve(filtered, msg.Candidates, msg.Config);
            Sender.Tell(new EpisodesResolved(resolved));
            return;
        }

        _tvdbPool.Tell(new FetchAndResolveEpisodes(msg.TvdbId, msg.Season, msg.Config, msg.Candidates), Sender);
    }

    private void HandleResolveMovie(ResolveMovie msg)
    {
        if (!_tmdbClient.IsConfigured)
        {
            Sender.Tell(new MovieResolutionFailed("TMDB API key is not configured"));
            return;
        }

        var tmdbId = msg.TmdbId;
        if (tmdbId is not null && _cache.TryGetValue(("tmdb", tmdbId.Value), out var cached) && !cached.IsExpired)
        {
            var movie = (TmdbMovie)cached.Data;
            var resolved = MovieResolver.Resolve(movie, [], msg.Candidates);
            Sender.Tell(new MoviesResolved(resolved));
            return;
        }

        _tmdbPool.Tell(new FetchAndResolveMovie(msg.ImdbId, msg.TmdbId, msg.Candidates), Sender);
    }

    private void HandleCacheUpdate(CacheUpdate msg)
    {
        var ttl = msg.Provider switch
        {
            "tvdb" when msg.Data is TvdbEpisode[] episodes => CacheTtl.DetermineShowTtl(episodes),
            "tmdb" => CacheTtl.Movie,
            _ => CacheTtl.Default,
        };

        _cache[(msg.Provider, msg.Id)] = new CacheEntry(msg.Data, DateTimeOffset.UtcNow, ttl, msg.Provider, msg.Id);
    }

    private void HandleCacheStats(QueryCacheStats _)
    {
        var tvdbCount = _cache.Count(kv => kv.Key.Provider == "tvdb");
        var tmdbCount = _cache.Count(kv => kv.Key.Provider == "tmdb");
        var oldest = _cache.Values.MinBy(e => e.FetchedAt)?.FetchedAt;
        Sender.Tell(new CacheStatsResult(tvdbCount, tmdbCount, oldest));
    }

    private static TvdbEpisode[] FilterBySeason(TvdbEpisode[] episodes, int? season)
    {
        if (season is null)
        {
            return episodes;
        }

        return episodes.Where(e => e.SeasonNumber == season.Value).ToArray();
    }
}
