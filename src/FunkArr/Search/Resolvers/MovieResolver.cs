using Akka.Actor;
using Akka.Event;
using Akka.Persistence;

namespace FunkArr.Search.Resolvers;

internal sealed record MovieResolverEvent(string Key, string Title, string? OriginalTitle, int? RuntimeMinutes, long TimestampUtcTicks);

internal sealed record MovieResolverSnapshot(Dictionary<string, CachedMovie> Movies);

internal sealed class MovieResolver : ReceivePersistentActor
{
    public override string PersistenceId => "movie-resolver";

    private const int SnapshotInterval = 500;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly TmdbClient _tmdbClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private readonly Dictionary<string, CachedMovie> _movieCache = new();
    private readonly Dictionary<string, List<IActorRef>> _pendingMovies = new();

    private long _eventCount;

    private sealed record MovieLookupResult(string Key, TmdbMovieInfo? Info);

    public MovieResolver(TmdbClient tmdbClient)
    {
        _tmdbClient = tmdbClient;

        Recovering();
    }

    private void Recovering()
    {
        Recover<MovieResolverEvent>(Apply);
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is MovieResolverSnapshot snapshot)
            {
                ApplySnapshot(snapshot);
            }
        });
        Recover<RecoveryCompleted>(_ =>
        {
            _log.Info("Recovery completed. {MovieCount} movies in cache", _movieCache.Count);
            Become(Ready);
        });
    }

    private void Ready()
    {
        Command<ResolveMovie>(HandleResolveMovie);
        Command<MovieLookupResult>(HandleMovieLookupResult);
        Command<SaveSnapshotSuccess>(msg =>
            DeleteMessages(msg.Metadata.SequenceNr));
        Command<SaveSnapshotFailure>(msg =>
            _log.Warning("Snapshot save failed: {Cause}", msg.Cause.Message));
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
            var evt = new MovieResolverEvent(
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

    private void Apply(MovieResolverEvent evt)
    {
        if (IsExpired(evt.TimestampUtcTicks))
        {
            return;
        }

        _movieCache[evt.Key] = new CachedMovie(evt.Title, evt.OriginalTitle, evt.RuntimeMinutes, evt.TimestampUtcTicks);
    }

    private void ApplySnapshot(MovieResolverSnapshot snapshot)
    {
        _movieCache.Clear();

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
            var snapshot = new MovieResolverSnapshot(new Dictionary<string, CachedMovie>(_movieCache));
            SaveSnapshot(snapshot);
        }
    }

    private static bool IsExpired(long timestampUtcTicks)
    {
        var age = DateTime.UtcNow.Ticks - timestampUtcTicks;
        return age > CacheTtl.Ticks;
    }
}
