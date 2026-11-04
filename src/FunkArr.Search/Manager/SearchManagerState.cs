using Akka.Actor;
using FunkArr.Messages;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Manager;

public sealed class SearchManagerState
{
    public static readonly SearchManagerState Empty = new();

    public Dictionary<Guid, PendingSearch> Pending { get; } = [];

    public sealed record PendingSearch(
        IActorRef OriginalSender,
        SearchSource Source,
        SearchManager.SearchType Type,
        SearchCommandCompleted? TvResult,
        SearchCommandCompleted? MovieResult);

    public sealed record AddSearch(Guid SearchId, PendingSearch Pending);

    public sealed record UpdateSearch(Guid SearchId, PendingSearch Pending);

    public sealed record RemoveSearch(Guid SearchId);

    public void Apply(AddSearch msg) => Pending[msg.SearchId] = msg.Pending;

    public void Apply(UpdateSearch msg) => Pending[msg.SearchId] = msg.Pending;

    public void Apply(RemoveSearch msg) => Pending.Remove(msg.SearchId);

    public PendingSearch? TryGetPending(Guid searchId)
        => Pending.GetValueOrDefault(searchId);

    public static SearchCommandCompleted MergeResults(Guid searchId, SearchCommandCompleted tv,
        SearchCommandCompleted movie)
    {
        var merged = tv.Items.Concat(movie.Items)
            .OrderByDescending(i => i.Score)
            .ToArray();
        return new SearchCommandCompleted(searchId, merged, merged.Length);
    }
}
