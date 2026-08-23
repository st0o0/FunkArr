using Akka.Cluster.Sharding;

namespace FunkArr.DownloadClient;

public sealed class DownloadCoordinatorMessageExtractor : HashCodeMessageExtractor
{
    public DownloadCoordinatorMessageExtractor() : base(maxNumberOfShards: 10) { }

    public override string? EntityId(object message) => message switch
    {
        IWithNzoId m => m.NzoId,
        _ => null,
    };
}
