using FunkArr.Messages;
using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Crawl;

internal sealed class FeedCrawlPageActorState
{
    public static FeedCrawlPageActorState Empty => new();

    private int _pendingGroups;
    private int _totalNewItems;
    private int _totalUnmatched;
    private bool _reachedEnd;
    private readonly List<FeedEntry> _accepted = [];
    private readonly Dictionary<RejectionReason, int> _rejectedByReason = [];

    public bool IsComplete => _pendingGroups == 0;

    public void ApplyFilterResult(int newItems, int unmatched, bool reachedEnd, int groupCount)
    {
        _totalNewItems = newItems;
        _totalUnmatched = unmatched;
        _reachedEnd = reachedEnd;
        _pendingGroups = groupCount;
    }

    public void ApplyGateResult(GateResult gate)
    {
        _accepted.AddRange(gate.Accepted);
        foreach (var (reason, count) in gate.RejectedByReason)
        {
            _rejectedByReason[reason] = _rejectedByReason.GetValueOrDefault(reason) + count;
        }

        _pendingGroups--;
    }

    public void ApplyEvalFailed()
    {
        _pendingGroups--;
    }

    public PageProcessed GetResult()
    {
        IReadOnlyDictionary<RejectionReason, int>? rejected = _rejectedByReason.Count > 0
            ? _rejectedByReason
            : null;

        return new PageProcessed([.. _accepted], _totalNewItems, _totalUnmatched, _reachedEnd, rejected, null);
    }
}
