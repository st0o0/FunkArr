using Akka.Actor;
using Akka.Event;
using Akka.Persistence;

namespace FunkArr.Search;

internal sealed record ShowResolvedEvent(int TvdbId, string ShowName, long TimestampUtcTicks);

internal sealed record MovieResolvedEvent(string Key, string Title, string? OriginalTitle, int? RuntimeMinutes, long TimestampUtcTicks);

internal sealed record ShowResolverSnapshot(
    Dictionary<int, CachedShow> Shows,
    Dictionary<string, CachedMovie> Movies);

internal sealed record CachedShow(string ShowName, long TimestampUtcTicks);

internal sealed record CachedMovie(string Title, string? OriginalTitle, int? RuntimeMinutes, long TimestampUtcTicks);

internal sealed class ShowResolverWorker : ReceivePersistentActor
{
    public override string PersistenceId => "show-resolver";

    private const int SnapshotInterval = 500;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly TvdbClient _tvdbClient;
    private readonly TmdbClient _tmdbClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private readonly Dictionary<int, CachedShow> _showCache = new();
    private readonly Dictionary<string, CachedMovie> _movieCache = new();

    private readonly Dictionary<int, List<IActorRef>> _pendingTvShows = new();
    private readonly Dictionary<string, List<IActorRef>> _pendingMovies = new();

    private long _eventCount;

    private sealed record TvShowLookupResult(int TvdbId, int? Season, TvdbShowInfo? ShowInfo, TvdbEpisodeInfo[]? Episodes);

    private sealed record MovieLookupResult(string Key, TmdbMovieInfo? Info);

    public ShowResolverWorker(TvdbClient tvdbClient, TmdbClient tmdbClient)
    {
        _tvdbClient = tvdbClient;
        _tmdbClient = tmdbClient;

        Recovering();
    }

    private void Recovering()
    {
        Recover<ShowResolvedEvent>(Apply);
        Recover<MovieResolvedEvent>(Apply);
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is ShowResolverSnapshot snapshot)
            {
                ApplySnapshot(snapshot);
            }
        });
        Recover<RecoveryCompleted>(_ =>
        {
            _log.Info("Recovery completed. {ShowCount} shows, {MovieCount} movies in cache",
                _showCache.Count, _movieCache.Count);
            Become(Ready);
        });
    }

    private void Ready()
    {
        Command<ResolveTvShow>(HandleResolveTvShow);
        Command<TvShowLookupResult>(HandleTvShowLookupResult);
        Command<ResolveMovie>(HandleResolveMovie);
        Command<MovieLookupResult>(HandleMovieLookupResult);
        Command<SaveSnapshotSuccess>(msg =>
            DeleteMessages(msg.Metadata.SequenceNr));
        Command<SaveSnapshotFailure>(msg =>
            _log.Warning("Snapshot save failed: {Cause}", msg.Cause.Message));
    }

    private void HandleResolveTvShow(ResolveTvShow cmd)
    {
        var sender = Sender;

        if (_showCache.TryGetValue(cmd.TvdbId, out var cached) && !IsExpired(cached.TimestampUtcTicks))
        {
            // Show name is cached; still need to fetch episodes (not cached)
            FetchEpisodes(cmd.TvdbId, cmd.Season, cached.ShowName, sender);
            return;
        }

        if (_pendingTvShows.TryGetValue(cmd.TvdbId, out var waiters))
        {
            waiters.Add(sender);
            return;
        }

        _pendingTvShows[cmd.TvdbId] = [sender];

        LookupTvShowAsync(cmd.TvdbId, cmd.Season)
            .PipeTo(Self, success: result => result);
    }

    private void HandleTvShowLookupResult(TvShowLookupResult result)
    {
        if (!_pendingTvShows.Remove(result.TvdbId, out var waiters))
        {
            return;
        }

        if (result.ShowInfo is not null)
        {
            var evt = new ShowResolvedEvent(
                result.TvdbId,
                result.ShowInfo.SeriesName,
                DateTime.UtcNow.Ticks);

            Persist(evt, persisted =>
            {
                Apply(persisted);
                IncrementAndSnapshot();

                var response = new TvShowResolved(result.ShowInfo.SeriesName, result.Episodes);
                foreach (var waiter in waiters)
                {
                    waiter.Tell(response);
                }
            });
        }
        else
        {
            var response = new TvShowResolved(null, result.Episodes);
            foreach (var waiter in waiters)
            {
                waiter.Tell(response);
            }
        }
    }

    private void HandleResolveMovie(ResolveMovie cmd)
    {
        var sender = Sender;
        var key = cmd.ImdbId ?? cmd.SearchTerm ?? string.Empty;

        if (string.IsNullOrEmpty(key))
        {
            sender.Tell(new MovieResolved(null));
            return;
        }

        if (_movieCache.TryGetValue(key, out var cached) && !IsExpired(cached.TimestampUtcTicks))
        {
            var info = new TmdbMovieInfo
            {
                Title = cached.Title,
                OriginalTitle = cached.OriginalTitle ?? string.Empty,
                RuntimeMinutes = cached.RuntimeMinutes,
            };
            sender.Tell(new MovieResolved(info));
            return;
        }

        if (_pendingMovies.TryGetValue(key, out var waiters))
        {
            waiters.Add(sender);
            return;
        }

        _pendingMovies[key] = [sender];

        LookupMovieAsync(key, cmd.ImdbId, cmd.SearchTerm)
            .PipeTo(Self, success: result => result);
    }

    private void HandleMovieLookupResult(MovieLookupResult result)
    {
        if (!_pendingMovies.Remove(result.Key, out var waiters))
        {
            return;
        }

        if (result.Info is not null)
        {
            var evt = new MovieResolvedEvent(
                result.Key,
                result.Info.Title,
                result.Info.OriginalTitle,
                result.Info.RuntimeMinutes,
                DateTime.UtcNow.Ticks);

            Persist(evt, persisted =>
            {
                Apply(persisted);
                IncrementAndSnapshot();

                var response = new MovieResolved(result.Info);
                foreach (var waiter in waiters)
                {
                    waiter.Tell(response);
                }
            });
        }
        else
        {
            var response = new MovieResolved(null);
            foreach (var waiter in waiters)
            {
                waiter.Tell(response);
            }
        }
    }

    private async Task<TvShowLookupResult> LookupTvShowAsync(int tvdbId, int? season)
    {
        var showInfo = await _tvdbClient.GetShowAsync(tvdbId);
        TvdbEpisodeInfo[]? episodes = null;

        if (season.HasValue)
        {
            episodes = await _tvdbClient.GetEpisodesAsync(tvdbId, season.Value);
        }

        return new TvShowLookupResult(tvdbId, season, showInfo, episodes);
    }

    private async Task<MovieLookupResult> LookupMovieAsync(string key, string? imdbId, string? searchTerm)
    {
        TmdbMovieInfo? info = null;

        if (!string.IsNullOrEmpty(imdbId))
        {
            info = await _tmdbClient.FindByImdbIdAsync(imdbId);
        }

        if (info is null && !string.IsNullOrEmpty(searchTerm))
        {
            info = await _tmdbClient.SearchMovieAsync(searchTerm);
        }

        return new MovieLookupResult(key, info);
    }

    private void FetchEpisodes(int tvdbId, int? season, string showName, IActorRef sender)
    {
        if (!season.HasValue)
        {
            sender.Tell(new TvShowResolved(showName, null));
            return;
        }

        _tvdbClient.GetEpisodesAsync(tvdbId, season.Value)
            .PipeTo(sender, success: episodes => new TvShowResolved(showName, episodes));
    }

    private void Apply(ShowResolvedEvent evt)
    {
        if (IsExpired(evt.TimestampUtcTicks))
        {
            return;
        }

        _showCache[evt.TvdbId] = new CachedShow(evt.ShowName, evt.TimestampUtcTicks);
    }

    private void Apply(MovieResolvedEvent evt)
    {
        if (IsExpired(evt.TimestampUtcTicks))
        {
            return;
        }

        _movieCache[evt.Key] = new CachedMovie(evt.Title, evt.OriginalTitle, evt.RuntimeMinutes, evt.TimestampUtcTicks);
    }

    private void ApplySnapshot(ShowResolverSnapshot snapshot)
    {
        _showCache.Clear();
        _movieCache.Clear();

        foreach (var (key, value) in snapshot.Shows)
        {
            if (!IsExpired(value.TimestampUtcTicks))
            {
                _showCache[key] = value;
            }
        }

        foreach (var (key, value) in snapshot.Movies)
        {
            if (!IsExpired(value.TimestampUtcTicks))
            {
                _movieCache[key] = value;
            }
        }
    }

    private void IncrementAndSnapshot()
    {
        _eventCount++;

        if (_eventCount % SnapshotInterval == 0)
        {
            var snapshot = new ShowResolverSnapshot(
                new Dictionary<int, CachedShow>(_showCache),
                new Dictionary<string, CachedMovie>(_movieCache));

            SaveSnapshot(snapshot);
        }
    }

    private static bool IsExpired(long timestampUtcTicks)
    {
        var age = DateTime.UtcNow.Ticks - timestampUtcTicks;
        return age > CacheTtl.Ticks;
    }
}
