using FunkArr.Core;
using Microsoft.AspNetCore.OutputCaching;

namespace FunkArr.ArrApi.Newznab;

public sealed class OutputCacheFeedInvalidator(IOutputCacheStore store) : IFeedCacheInvalidator
{
    public Task InvalidateAsync(CancellationToken cancellationToken) =>
        store.EvictByTagAsync(NewznabFeedCache.Tag, cancellationToken).AsTask();
}
