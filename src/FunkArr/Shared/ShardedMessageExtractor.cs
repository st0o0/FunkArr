using Akka.Cluster.Sharding;

namespace FunkArr.Shared;

public sealed class ShardedMessageExtractor(int maxNumberOfShards)
    : HashCodeMessageExtractor(maxNumberOfShards)
{
    public override string? EntityId(object message) => message switch
    {
        IShardedMessage m => m.EntityKey,
        _ => null,
    };
}
