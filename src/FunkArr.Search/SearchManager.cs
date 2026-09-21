using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages.Search;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class SearchManager : ReceiveActor, IWithTimers
{
    public enum SearchType { Tv, Movie, Both }

    private sealed record SearchTimeout(Guid SearchId);

    private readonly ILoggingAdapter _log = Context.GetLogger();
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

        Receive<SearchCommand>(HandleSearch);
        Receive<SearchSeriesCompleted>(msg => HandleCompleted(msg.SearchId, msg.Items, msg.Total));
        Receive<SearchMovieCompleted>(msg => HandleCompleted(msg.SearchId, msg.Items, msg.Total));
        Receive<SearchSeriesFailed>(msg => HandleFailed(msg.SearchId, msg.Cause));
        Receive<SearchMovieFailed>(msg => HandleFailed(msg.SearchId, msg.Cause));
        Receive<SearchTimeout>(HandleTimeout);
    }

    private void HandleSearch(SearchCommand cmd)
    {
        var searchId = Guid.NewGuid();

        switch (cmd.Params)
        {
            case SearchCommand.TvParams tv:
                _tvShardRegion.Tell(new SearchSeries(searchId, cmd.Source, cmd.Query,
                    tv.Season, tv.Episode, tv.TvdbId, tv.ImdbId,
                    cmd.Limit, cmd.Offset));
                _state = _state.Apply(new SearchManagerState.AddSearch(searchId,
                    new SearchManagerState.PendingSearch(Sender, SearchType.Tv, null, null)));
                break;

            case SearchCommand.MovieParams movie:
                _movieShardRegion.Tell(new SearchMovie(searchId, cmd.Source, cmd.Query,
                    movie.ImdbId, movie.TmdbId,
                    cmd.Limit, cmd.Offset));
                _state = _state.Apply(new SearchManagerState.AddSearch(searchId,
                    new SearchManagerState.PendingSearch(Sender, SearchType.Movie, null, null)));
                break;

            default:
                RouteByCategory(searchId, cmd);
                break;
        }

        ScheduleTimeout(searchId);
    }

    private void RouteByCategory(Guid searchId, SearchCommand cmd)
    {
        var type = cmd.Cat switch
        {
            >= 5000 and < 6000 => SearchType.Tv,
            >= 2000 and < 3000 => SearchType.Movie,
            _ => SearchType.Both,
        };

        switch (type)
        {
            case SearchType.Tv:
                _tvShardRegion.Tell(new SearchSeries(searchId, cmd.Source, cmd.Query,
                    null, null, null, null, cmd.Limit, cmd.Offset));
                _state = _state.Apply(new SearchManagerState.AddSearch(searchId,
                    new SearchManagerState.PendingSearch(Sender, SearchType.Tv, null, null)));
                break;

            case SearchType.Movie:
                _movieShardRegion.Tell(new SearchMovie(searchId, cmd.Source, cmd.Query,
                    null, null, cmd.Limit, cmd.Offset));
                _state = _state.Apply(new SearchManagerState.AddSearch(searchId,
                    new SearchManagerState.PendingSearch(Sender, SearchType.Movie, null, null)));
                break;

            default:
                _tvShardRegion.Tell(new SearchSeries(searchId, cmd.Source, cmd.Query,
                    null, null, null, null, cmd.Limit, cmd.Offset));
                _movieShardRegion.Tell(new SearchMovie(searchId, cmd.Source, cmd.Query,
                    null, null, cmd.Limit, cmd.Offset));
                _state = _state.Apply(new SearchManagerState.AddSearch(searchId,
                    new SearchManagerState.PendingSearch(Sender, SearchType.Both, null, null)));
                break;
        }
    }

    private void HandleCompleted(Guid searchId, SearchResultItem[] items, int total)
    {
        var pending = _state.TryGetPending(searchId);
        if (pending is null)
        {
            return;
        }

        var completed = new SearchCommandCompleted(searchId, items, total);

        switch (pending.Type)
        {
            case SearchType.Tv:
            case SearchType.Movie:
                pending.OriginalSender.Tell(completed);
                _state = _state.Apply(new SearchManagerState.RemoveSearch(searchId));
                break;

            case SearchType.Both:
                var updated = pending.TvResult is null
                    ? pending with { TvResult = completed }
                    : pending with { MovieResult = completed };

                if (updated.TvResult is not null && updated.MovieResult is not null)
                {
                    var merged = SearchManagerStateExtensions.MergeResults(
                        searchId, updated.TvResult, updated.MovieResult);
                    pending.OriginalSender.Tell(merged);
                    _state = _state.Apply(new SearchManagerState.RemoveSearch(searchId));
                }
                else
                {
                    _state = _state.Apply(new SearchManagerState.UpdateSearch(searchId, updated));
                }
                break;
        }
    }

    private void HandleFailed(Guid searchId, Exception cause)
    {
        var pending = _state.TryGetPending(searchId);
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
                _state = _state.Apply(new SearchManagerState.RemoveSearch(searchId));
                return;
            }
        }

        pending.OriginalSender.Tell(new SearchCommandFailed(searchId, cause));
        _state = _state.Apply(new SearchManagerState.RemoveSearch(searchId));
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
                _state = _state.Apply(new SearchManagerState.RemoveSearch(timeout.SearchId));
                return;
            }
        }

        _log.Warning("Search {SearchId} timed out after {Timeout}s", timeout.SearchId, _searchTimeout.TotalSeconds);
        pending.OriginalSender.Tell(new SearchCommandFailed(timeout.SearchId, new TimeoutException("Search timed out")));
        _state = _state.Apply(new SearchManagerState.RemoveSearch(timeout.SearchId));
    }

    private void ScheduleTimeout(Guid searchId) => Timers.StartSingleTimer($"timeout-{searchId}", new SearchTimeout(searchId), _searchTimeout);
}
