using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadManagerRetryStateTests
{
    [Fact]
    public void Re_enqueue_after_dequeue_adds_back_to_queue()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id))
            .Apply(new DownloadDispatched(id))
            .Apply(new DownloadDequeued(id))
            .Apply(new DownloadEnqueued(id));

        Assert.Single(state.Queued);
        Assert.Equal(id, state.Queued[0]);
        Assert.Empty(state.Dispatched);
    }

    [Fact]
    public void Re_enqueued_item_is_found_by_contains()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id))
            .Apply(new DownloadDispatched(id))
            .Apply(new DownloadDequeued(id));

        Assert.False(state.Contains(id));

        state = state.Apply(new DownloadEnqueued(id));
        Assert.True(state.Contains(id));
    }

    [Fact]
    public void Re_enqueued_item_goes_to_end_of_queue()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id1))
            .Apply(new DownloadDispatched(id1))
            .Apply(new DownloadDequeued(id1))
            .Apply(new DownloadEnqueued(id2))
            .Apply(new DownloadEnqueued(id1));

        Assert.Equal(2, state.Queued.Count);
        Assert.Equal(id2, state.Queued[0]);
        Assert.Equal(id1, state.Queued[1]);
    }

    [Fact]
    public void Re_enqueued_item_can_be_dispatched_again()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id))
            .Apply(new DownloadDispatched(id))
            .Apply(new DownloadDequeued(id))
            .Apply(new DownloadEnqueued(id))
            .Apply(new DownloadDispatched(id));

        Assert.Empty(state.Queued);
        Assert.Contains(id, state.Dispatched);
    }
}
