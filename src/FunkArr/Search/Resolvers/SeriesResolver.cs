using Akka.Actor;
using Akka.Event;
using Akka.Persistence;

namespace FunkArr.Search.Resolvers;

internal sealed record SeriesResolvedEvent(int TvdbId, string ShowName, long TimestampUtcTicks);

internal sealed record SeriesResolverSnapshot(Dictionary<int, CachedShow> Shows);

internal sealed class SeriesResolver : ReceivePersistentActor
{
    public override string PersistenceId => "series-resolver";

    private const int SnapshotInterval = 500;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly TvdbClient _tvdbClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private readonly Dictionary<int, CachedShow> _showCache = new();
    private readonly Dictionary<int, List<IActorRef>> _pendingTvShows = new();

    private long _eventCount;

    private sealed record TvShowLookupResult(int TvdbId, int? Season, TvdbShowInfo? ShowInfo, TvdbEpisodeInfo[]? Episodes);

    public SeriesResolver(TvdbClient tvdbClient)
    {
        _tvdbClient = tvdbClient;

        Recovering();
    }

    private void Recovering()
    {
        Recover<SeriesResolvedEvent>(Apply);
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is SeriesResolverSnapshot snapshot)
            {
                ApplySnapshot(snapshot);
            }
        });
        Recover<RecoveryCompleted>(_ =>
        {
            _log.Info("Recovery completed. {ShowCount} shows in cache", _showCache.Count);
            Become(Ready);
        });
    }

    private void Ready()
    {
        Command<ResolveTvShow>(HandleResolveTvShow);
        Command<TvShowLookupResult>(HandleTvShowLookupResult);
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
            var evt = new SeriesResolvedEvent(
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

    private void Apply(SeriesResolvedEvent evt)
    {
        if (IsExpired(evt.TimestampUtcTicks))
        {
            return;
        }

        _showCache[evt.TvdbId] = new CachedShow(evt.ShowName, evt.TimestampUtcTicks);
    }

    private void ApplySnapshot(SeriesResolverSnapshot snapshot)
    {
        _showCache.Clear();

        foreach (var (key, value) in snapshot.Shows)
        {
            if (!IsExpired(value.TimestampUtcTicks))
            {
                _showCache[key] = value;
            }
        }
    }

    private void IncrementAndSnapshot()
    {
        _eventCount++;

        if (_eventCount % SnapshotInterval == 0)
        {
            var snapshot = new SeriesResolverSnapshot(
                new Dictionary<int, CachedShow>(_showCache));

            SaveSnapshot(snapshot);
        }
    }

    private static bool IsExpired(long timestampUtcTicks)
    {
        var age = DateTime.UtcNow.Ticks - timestampUtcTicks;
        return age > CacheTtl.Ticks;
    }
}
