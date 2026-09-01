using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages.Search;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class SearchManager : ReceiveActor, IWithTimers
{
    public enum SearchType { Tv, Movie, Both }

    public sealed record PendingSearch(
        IActorRef OriginalSender,
        SearchType Type,
        SearchCompleted? TvResult,
        SearchCompleted? MovieResult);

    private sealed record SearchTimeout(Guid SearchId);

    private readonly IActorRef _tvShardRegion;
    private readonly IActorRef _movieShardRegion;
    private readonly TimeSpan _searchTimeout;
    private readonly Dictionary<Guid, PendingSearch> _pending = new();

    public ITimerScheduler Timers { get; set; } = null!;

    public SearchManager(TimeSpan? searchTimeout = null)
    {
        _tvShardRegion = Context.GetActor<ITvSearchRegion>();
        _movieShardRegion = Context.GetActor<IMovieSearchRegion>();
        _searchTimeout = searchTimeout ?? TimeSpan.FromSeconds(30);

        Receive<TvSearchCommand>(HandleTvSearch);
        Receive<MovieSearchCommand>(HandleMovieSearch);
        Receive<GeneralSearchCommand>(HandleGeneralSearch);
        Receive<SearchCompleted>(HandleSearchCompleted);
        Receive<SearchFailed>(HandleSearchFailed);
        Receive<SearchTimeout>(HandleTimeout);
    }

    private void HandleTvSearch(TvSearchCommand cmd)
    {
        var searchId = Guid.NewGuid();
        var tvCmd = cmd with { SearchId = searchId };

        _pending[searchId] = new PendingSearch(Sender, SearchType.Tv, null, null);
        _tvShardRegion.Tell(tvCmd);
        ScheduleTimeout(searchId);
    }

    private void HandleMovieSearch(MovieSearchCommand cmd)
    {
        var searchId = Guid.NewGuid();
        var movieCmd = cmd with { SearchId = searchId };

        _pending[searchId] = new PendingSearch(Sender, SearchType.Movie, null, null);
        _movieShardRegion.Tell(movieCmd);
        ScheduleTimeout(searchId);
    }

    private void HandleGeneralSearch(GeneralSearchCommand cmd)
    {
        var searchId = Guid.NewGuid();
        _pending[searchId] = DetermineSearchType(cmd.Cat) switch
        {
            SearchType.Tv => RouteToTv(searchId, cmd),
            SearchType.Movie => RouteToMovie(searchId, cmd),
            _ => RouteToAll(searchId, cmd),
        };
        ScheduleTimeout(searchId);
    }

    private static SearchType DetermineSearchType(int? cat) => cat switch
    {
        >= 5000 and < 6000 => SearchType.Tv,
        >= 2000 and < 3000 => SearchType.Movie,
        _ => SearchType.Both,
    };

    private PendingSearch RouteToTv(Guid searchId, GeneralSearchCommand cmd)
    {
        _tvShardRegion.Tell(new TvSearchCommand(searchId, cmd.Query, null, null, null, null));
        return new PendingSearch(Sender, SearchType.Tv, null, null);
    }

    private PendingSearch RouteToMovie(Guid searchId, GeneralSearchCommand cmd)
    {
        _movieShardRegion.Tell(new MovieSearchCommand(searchId, cmd.Query, null, null));
        return new PendingSearch(Sender, SearchType.Movie, null, null);
    }

    private PendingSearch RouteToAll(Guid searchId, GeneralSearchCommand cmd)
    {
        _tvShardRegion.Tell(new TvSearchCommand(searchId, cmd.Query, null, null, null, null));
        _movieShardRegion.Tell(new MovieSearchCommand(searchId, cmd.Query, null, null));
        return new PendingSearch(Sender, SearchType.Both, null, null);
    }

    private void HandleSearchCompleted(SearchCompleted completed)
    {
        if (!_pending.TryGetValue(completed.SearchId, out var pending))
        {
            return;
        }

        switch (pending.Type)
        {
            case SearchType.Tv:
            case SearchType.Movie:
                pending.OriginalSender.Tell(completed);
                _pending.Remove(completed.SearchId);
                break;

            case SearchType.Both:
                var updated = pending.TvResult is null
                    ? pending with { TvResult = completed }
                    : pending with { MovieResult = completed };

                if (updated.TvResult is not null && updated.MovieResult is not null)
                {
                    var merged = MergeResults(completed.SearchId, updated.TvResult, updated.MovieResult);
                    pending.OriginalSender.Tell(merged);
                    _pending.Remove(completed.SearchId);
                }
                else
                {
                    _pending[completed.SearchId] = updated;
                }
                break;
        }
    }

    private void HandleSearchFailed(SearchFailed failed)
    {
        if (!_pending.TryGetValue(failed.SearchId, out var pending))
        {
            return;
        }

        if (pending.Type == SearchType.Both)
        {
            var partial = pending.TvResult ?? pending.MovieResult;
            if (partial is not null)
            {
                pending.OriginalSender.Tell(partial);
                _pending.Remove(failed.SearchId);
                return;
            }
        }

        pending.OriginalSender.Tell(failed);
        _pending.Remove(failed.SearchId);
    }

    private void HandleTimeout(SearchTimeout timeout)
    {
        if (!_pending.TryGetValue(timeout.SearchId, out var pending))
        {
            return;
        }

        if (pending.Type == SearchType.Both)
        {
            var partial = pending.TvResult ?? pending.MovieResult;
            if (partial is not null)
            {
                pending.OriginalSender.Tell(partial);
                _pending.Remove(timeout.SearchId);
                return;
            }
        }

        pending.OriginalSender.Tell(new SearchFailed(timeout.SearchId, "Search timed out"));
        _pending.Remove(timeout.SearchId);
    }

    private static SearchCompleted MergeResults(Guid searchId, SearchCompleted tv, SearchCompleted movie)
    {
        var merged = tv.Items.Concat(movie.Items)
            .OrderByDescending(i => i.Score)
            .ToArray();
        return new SearchCompleted(searchId, merged, merged.Length);
    }

    private void ScheduleTimeout(Guid searchId)
    {
        Timers.StartSingleTimer($"timeout-{searchId}", new SearchTimeout(searchId), _searchTimeout);
    }
}
