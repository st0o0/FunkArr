using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadManagerRetryStateTests
{
    private static DownloadEnqueued Enqueue(Guid id) => new(id, PersistedDownloadPriority.Normal);

    [Fact]
    public void Re_enqueue_after_dequeue_adds_back_to_queue()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id))
            .Apply(new DownloadDispatched(id))
            .Apply(new DownloadDequeued(id))
            .Apply(Enqueue(id));

        Assert.Single(state.Queued);
        Assert.Equal(id, state.Queued[0].Id);
        Assert.Empty(state.Dispatched);
    }

    [Fact]
    public void Re_enqueued_item_is_found_by_contains()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id))
            .Apply(new DownloadDispatched(id))
            .Apply(new DownloadDequeued(id));

        Assert.False(state.Contains(id));

        state = state.Apply(Enqueue(id));
        Assert.True(state.Contains(id));
    }

    [Fact]
    public void Re_enqueued_item_goes_to_end_of_queue()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id1))
            .Apply(new DownloadDispatched(id1))
            .Apply(new DownloadDequeued(id1))
            .Apply(Enqueue(id2))
            .Apply(Enqueue(id1));

        Assert.Equal(2, state.Queued.Count);
        Assert.Equal(id2, state.Queued[0].Id);
        Assert.Equal(id1, state.Queued[1].Id);
    }

    [Fact]
    public void Re_enqueued_item_can_be_dispatched_again()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id))
            .Apply(new DownloadDispatched(id))
            .Apply(new DownloadDequeued(id))
            .Apply(Enqueue(id))
            .Apply(new DownloadDispatched(id));

        Assert.Empty(state.Queued);
        Assert.True(state.Dispatched.ContainsKey(id));
    }
}
