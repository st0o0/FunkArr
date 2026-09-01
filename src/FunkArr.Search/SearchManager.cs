using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages.Search;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class SearchManager : ReceiveActor, IWithTimers
{
    public enum SearchType { Tv, Movie, Both }

    private sealed record SearchTimeout(Guid SearchId);

    private readonly IActorRef _tvShardRegion;
    private readonly IActorRef _movieShardRegion;
    private readonly TimeSpan _searchTimeout;
    private SearchManagerState _state = SearchManagerState.Empty;

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

        _state = _state.AddPending(searchId,
            new SearchManagerState.PendingSearch(Sender, SearchType.Tv, null, null));
        _tvShardRegion.Tell(tvCmd);
        ScheduleTimeout(searchId);
    }

    private void HandleMovieSearch(MovieSearchCommand cmd)
    {
        var searchId = Guid.NewGuid();
        var movieCmd = cmd with { SearchId = searchId };

        _state = _state.AddPending(searchId,
            new SearchManagerState.PendingSearch(Sender, SearchType.Movie, null, null));
        _movieShardRegion.Tell(movieCmd);
        ScheduleTimeout(searchId);
    }

    private void HandleGeneralSearch(GeneralSearchCommand cmd)
    {
        var searchId = Guid.NewGuid();
        var type = DetermineSearchType(cmd.Cat);

        _state = type switch
        {
            SearchType.Tv => RouteTv(searchId, cmd),
            SearchType.Movie => RouteMovie(searchId, cmd),
            _ => RouteAll(searchId, cmd),
        };

        ScheduleTimeout(searchId);
    }

    private static SearchType DetermineSearchType(int? cat) => cat switch
    {
        >= 5000 and < 6000 => SearchType.Tv,
        >= 2000 and < 3000 => SearchType.Movie,
        _ => SearchType.Both,
    };

    private SearchManagerState RouteTv(Guid searchId, GeneralSearchCommand cmd)
    {
        _tvShardRegion.Tell(new TvSearchCommand(searchId, cmd.Query, null, null, null, null, cmd.Limit, cmd.Offset));
        return _state.AddPending(searchId,
            new SearchManagerState.PendingSearch(Sender, SearchType.Tv, null, null));
    }

    private SearchManagerState RouteMovie(Guid searchId, GeneralSearchCommand cmd)
    {
        _movieShardRegion.Tell(new MovieSearchCommand(searchId, cmd.Query, null, null, cmd.Limit, cmd.Offset));
        return _state.AddPending(searchId,
            new SearchManagerState.PendingSearch(Sender, SearchType.Movie, null, null));
    }

    private SearchManagerState RouteAll(Guid searchId, GeneralSearchCommand cmd)
    {
        _tvShardRegion.Tell(new TvSearchCommand(searchId, cmd.Query, null, null, null, null, cmd.Limit, cmd.Offset));
        _movieShardRegion.Tell(new MovieSearchCommand(searchId, cmd.Query, null, null, cmd.Limit, cmd.Offset));
        return _state.AddPending(searchId,
            new SearchManagerState.PendingSearch(Sender, SearchType.Both, null, null));
    }

    private void HandleSearchCompleted(SearchCompleted completed)
    {
        var pending = _state.TryGetPending(completed.SearchId);
        if (pending is null)
        {
            return;
        }

        switch (pending.Type)
        {
            case SearchType.Tv:
            case SearchType.Movie:
                pending.OriginalSender.Tell(completed);
                _state = _state.RemovePending(completed.SearchId);
                break;

            case SearchType.Both:
                var updated = pending.TvResult is null
                    ? pending with { TvResult = completed }
                    : pending with { MovieResult = completed };

                if (updated.TvResult is not null && updated.MovieResult is not null)
                {
                    var merged = SearchManagerStateExtensions.MergeResults(
                        completed.SearchId, updated.TvResult, updated.MovieResult);
                    pending.OriginalSender.Tell(merged);
                    _state = _state.RemovePending(completed.SearchId);
                }
                else
                {
                    _state = _state.UpdatePending(completed.SearchId, updated);
                }
                break;
        }
    }

    private void HandleSearchFailed(SearchFailed failed)
    {
        var pending = _state.TryGetPending(failed.SearchId);
        if (pending is null)
        {
            return;
        }

        if (pending.Type == SearchType.Both)
        {
            var partial = pending.TvResult ?? pending.MovieResult;
            if (partial is not null)
            {
                pending.OriginalSender.Tell(partial);
                _state = _state.RemovePending(failed.SearchId);
                return;
            }
        }

        pending.OriginalSender.Tell(failed);
        _state = _state.RemovePending(failed.SearchId);
    }

    private void HandleTimeout(SearchTimeout timeout)
    {
        var pending = _state.TryGetPending(timeout.SearchId);
        if (pending is null)
        {
            return;
        }

        if (pending.Type == SearchType.Both)
        {
            var partial = pending.TvResult ?? pending.MovieResult;
            if (partial is not null)
            {
                pending.OriginalSender.Tell(partial);
                _state = _state.RemovePending(timeout.SearchId);
                return;
            }
        }

        pending.OriginalSender.Tell(new SearchFailed(timeout.SearchId, "Search timed out"));
        _state = _state.RemovePending(timeout.SearchId);
    }

    private void ScheduleTimeout(Guid searchId)
    {
        Timers.StartSingleTimer($"timeout-{searchId}", new SearchTimeout(searchId), _searchTimeout);
    }
}
