using System.Collections.Immutable;
using Akka.Actor;
using FunkArr.Messages.Search;

namespace FunkArr.Search;

public sealed record SearchManagerState(
    ImmutableDictionary<Guid, SearchManagerState.PendingSearch> Pending)
{
    public static readonly SearchManagerState Empty = new(
        ImmutableDictionary<Guid, PendingSearch>.Empty);

    public sealed record PendingSearch(
        IActorRef OriginalSender,
        SearchManager.SearchType Type,
        SearchCommandCompleted? TvResult,
        SearchCommandCompleted? MovieResult);
}

public static class SearchManagerStateExtensions
{
    public static SearchManagerState AddPending(this SearchManagerState state, Guid searchId,
        SearchManagerState.PendingSearch pending) =>
        new(Pending: state.Pending.SetItem(searchId, pending));

    public static SearchManagerState UpdatePending(this SearchManagerState state, Guid searchId,
        SearchManagerState.PendingSearch pending) =>
        new(Pending: state.Pending.SetItem(searchId, pending));

    public static SearchManagerState RemovePending(this SearchManagerState state, Guid searchId) =>
        new(Pending: state.Pending.Remove(searchId));

    public static SearchManagerState.PendingSearch? TryGetPending(
        this SearchManagerState state, Guid searchId) =>
        state.Pending.TryGetValue(searchId, out var pending) ? pending : null;

    public static SearchCommandCompleted MergeResults(Guid searchId, SearchCommandCompleted tv, SearchCommandCompleted movie)
    {
        var merged = tv.Items.Concat(movie.Items)
            .OrderByDescending(i => i.Score)
            .ToArray();
        return new SearchCommandCompleted(searchId, merged, merged.Length);
    }
}
