namespace FunkArr.Core;

public interface IFeedCacheInvalidator
{
    Task InvalidateAsync(CancellationToken cancellationToken);
}
