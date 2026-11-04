using Akka.Actor;

namespace FunkArr.Search;

public sealed class MediathekViewWebManagerState
{
    public static readonly MediathekViewWebManagerState Empty = new();

    public Dictionary<Guid, IActorRef> Pending { get; } = [];
}
