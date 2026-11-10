using FunkArr.Messages.Feed;

namespace FunkArr.Feed.Crawl;

internal sealed record FilterResult(int NewItems, int Unmatched, bool ReachedEnd, int GroupCount);

internal sealed record EvalFailure;

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

    public void Apply(FilterResult result)
    {
        _totalNewItems = result.NewItems;
        _totalUnmatched = result.Unmatched;
        _reachedEnd = result.ReachedEnd;
        _pendingGroups = result.GroupCount;
    }

    public void Apply(GateResult gate)
    {
        _accepted.AddRange(gate.Accepted);
        foreach (var (reason, count) in gate.RejectedByReason)
        {
            _rejectedByReason[reason] = _rejectedByReason.GetValueOrDefault(reason) + count;
        }

        _pendingGroups--;
    }

    public void Apply(EvalFailure _) => _pendingGroups--;

    public ProcessPageCompleted GetResult() =>
        new([.. _accepted], _totalNewItems, _totalUnmatched, _reachedEnd, _rejectedByReason);
}
