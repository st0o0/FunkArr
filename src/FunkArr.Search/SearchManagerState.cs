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

    public sealed record AddSearch(Guid SearchId, PendingSearch Pending);
    public sealed record UpdateSearch(Guid SearchId, PendingSearch Pending);
    public sealed record RemoveSearch(Guid SearchId);
}

public static class SearchManagerStateExtensions
{
    public static SearchManagerState Apply(this SearchManagerState state, SearchManagerState.AddSearch msg) =>
        new(Pending: state.Pending.SetItem(msg.SearchId, msg.Pending));

    public static SearchManagerState Apply(this SearchManagerState state, SearchManagerState.UpdateSearch msg) =>
        new(Pending: state.Pending.SetItem(msg.SearchId, msg.Pending));

    public static SearchManagerState Apply(this SearchManagerState state, SearchManagerState.RemoveSearch msg) =>
        new(Pending: state.Pending.Remove(msg.SearchId));

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
