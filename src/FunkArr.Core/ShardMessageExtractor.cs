using Akka.Cluster.Sharding;
using FunkArr.Messages;

namespace FunkArr.Core;

public sealed class ShardMessageExtractor(int maxShards = 25) : HashCodeMessageExtractor(maxShards)
{
    public override string EntityId(object message) => message switch
    {
        IWithDownloadId m => m.DownloadId.ToString(),
        IWithSearchId m => m.SearchId.ToString(),
        _ => throw new ArgumentException($"Unknown sharded message type: {message.GetType().Name}", nameof(message)),
    };
}
