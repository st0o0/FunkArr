using System.Diagnostics.CodeAnalysis;
using FunkArr.Messages;
using FunkArr.Messages.Feed;

namespace FunkArr.ArrApi.Newznab;

public sealed class FeedResultCache
{
    private readonly Lock _gate = new();
    private readonly Dictionary<FeedPageKey, FeedResult> _pages = [];
    private long _generation;

    public bool TryGet(FeedPageKey key, [NotNullWhen(true)] out FeedResult? result)
    {
        lock (_gate)
        {
            return _pages.TryGetValue(key, out result);
        }
    }

    public long Generation
    {
        get
        {
            lock (_gate)
            {
                return _generation;
            }
        }
    }

    public void Store(FeedPageKey key, FeedResult result, long generation)
    {
        lock (_gate)
        {
            if (generation == _generation)
            {
                _pages[key] = result;
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _generation++;
            _pages.Clear();
        }
    }
}

public sealed record FeedPageKey(MediaType? MediaType, int Offset, int Limit);
