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
        SearchCompleted? TvResult,
        SearchCompleted? MovieResult);
}

public static class SearchManagerStateExtensions
{
    public static SearchManagerState AddPending(
        this SearchManagerState state,
        Guid searchId,
        SearchManagerState.PendingSearch pending) =>
        state with { Pending = state.Pending.SetItem(searchId, pending) };

    public static SearchManagerState UpdatePending(
        this SearchManagerState state,
        Guid searchId,
        SearchManagerState.PendingSearch pending) =>
        state with { Pending = state.Pending.SetItem(searchId, pending) };

    public static SearchManagerState RemovePending(
        this SearchManagerState state, Guid searchId) =>
        state with { Pending = state.Pending.Remove(searchId) };

    public static SearchManagerState.PendingSearch? TryGetPending(
        this SearchManagerState state, Guid searchId) =>
        state.Pending.TryGetValue(searchId, out var pending) ? pending : null;

    public static SearchCompleted MergeResults(Guid searchId, SearchCompleted tv, SearchCompleted movie)
    {
        var merged = tv.Items.Concat(movie.Items)
            .OrderByDescending(i => i.Score)
            .ToArray();
        return new SearchCompleted(searchId, merged, merged.Length);
    }
}
