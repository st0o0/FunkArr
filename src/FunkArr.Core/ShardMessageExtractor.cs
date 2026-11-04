using Akka.Cluster.Sharding;
using Terp.Akka;

namespace FunkArr.Core;

public sealed class ShardMessageExtractor(int maxShards = 5) : HashCodeMessageExtractor(maxShards)
{
    public override string EntityId(object message) => message switch
    {
        IWithEntityId m => m.EntityId,
        _ => throw new ArgumentException($"Unknown sharded message type: {message.GetType().Name}", nameof(message))
    };
}
