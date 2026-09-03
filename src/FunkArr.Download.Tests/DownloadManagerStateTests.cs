using FunkArr.Download;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadManagerStateTests
{
    [Fact]
    public void Apply_Enqueued_adds_to_queue()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id));

        Assert.Single(state.Queued);
        Assert.Equal(id, state.Queued[0]);
        Assert.Empty(state.Dispatched);
    }

    [Fact]
    public void Apply_Dispatched_moves_from_queued_to_dispatched()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id))
            .Apply(new DownloadDispatched(id));

        Assert.Empty(state.Queued);
        Assert.Contains(id, state.Dispatched);
    }

    [Fact]
    public void Apply_Dequeued_removes_from_dispatched()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id))
            .Apply(new DownloadDispatched(id))
            .Apply(new DownloadDequeued(id));

        Assert.Empty(state.Queued);
        Assert.Empty(state.Dispatched);
    }

    [Fact]
    public void Apply_Dequeued_removes_from_queued()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id))
            .Apply(new DownloadDequeued(id));

        Assert.Empty(state.Queued);
        Assert.Empty(state.Dispatched);
    }

    [Fact]
    public void ResetDispatched_moves_dispatched_to_front_of_queue()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id1))
            .Apply(new DownloadEnqueued(id2))
            .Apply(new DownloadDispatched(id1))
            .ResetDispatched();

        Assert.Equal(2, state.Queued.Count);
        Assert.Equal(id1, state.Queued[0]);
        Assert.Equal(id2, state.Queued[1]);
        Assert.Empty(state.Dispatched);
    }

    [Fact]
    public void Contains_finds_queued_items()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id));

        Assert.True(state.Contains(id));
        Assert.False(state.Contains(Guid.NewGuid()));
    }

    [Fact]
    public void Contains_finds_dispatched_items()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id))
            .Apply(new DownloadDispatched(id));

        Assert.True(state.Contains(id));
    }

    [Fact]
    public void Queue_preserves_insertion_order()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id1))
            .Apply(new DownloadEnqueued(id2))
            .Apply(new DownloadEnqueued(id3));

        Assert.Equal(3, state.Queued.Count);
        Assert.Equal(id1, state.Queued[0]);
        Assert.Equal(id2, state.Queued[1]);
        Assert.Equal(id3, state.Queued[2]);
    }

    [Fact]
    public void Multiple_dispatches_respected()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id1))
            .Apply(new DownloadEnqueued(id2))
            .Apply(new DownloadEnqueued(id3))
            .Apply(new DownloadDispatched(id1))
            .Apply(new DownloadDispatched(id2));

        Assert.Single(state.Queued);
        Assert.Equal(id3, state.Queued[0]);
        Assert.Equal(2, state.Dispatched.Count);
    }

    private static QueueItem MakeItem(string title, string category = "tv") =>
        new(Guid.NewGuid(), title, DownloadStatus.Queued, 1000, 0, 0, 100, 0, category);

    [Fact]
    public void PaginateQueue_returns_all_when_limit_zero()
    {
        var items = new[] { MakeItem("A"), MakeItem("B"), MakeItem("C") };

        var result = DownloadManagerStateExtensions.PaginateQueue(items, new QueryQueue(), 3);

        Assert.Equal(3, result.Items.Length);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(3, result.TotalSlots);
    }

    [Fact]
    public void PaginateQueue_applies_start_and_limit()
    {
        var items = new[] { MakeItem("A"), MakeItem("B"), MakeItem("C"), MakeItem("D") };

        var result = DownloadManagerStateExtensions.PaginateQueue(items, new QueryQueue(Start: 1, Limit: 2), 3);

        Assert.Equal(2, result.Items.Length);
        Assert.Equal("B", result.Items[0].Title);
        Assert.Equal("C", result.Items[1].Title);
        Assert.Equal(4, result.TotalItems);
    }

    [Fact]
    public void PaginateQueue_filters_by_category()
    {
        var items = new[] { MakeItem("A", "tv"), MakeItem("B", "movies"), MakeItem("C", "tv") };

        var result = DownloadManagerStateExtensions.PaginateQueue(items, new QueryQueue(Category: "tv"), 3);

        Assert.Equal(2, result.Items.Length);
        Assert.Equal(2, result.TotalItems);
    }

    [Fact]
    public void PaginateQueue_category_filter_is_case_insensitive()
    {
        var items = new[] { MakeItem("A", "TV"), MakeItem("B", "movies") };

        var result = DownloadManagerStateExtensions.PaginateQueue(items, new QueryQueue(Category: "tv"), 3);

        Assert.Single(result.Items);
    }

    [Fact]
    public void PaginateQueue_category_filter_with_pagination()
    {
        var items = new[] { MakeItem("A", "tv"), MakeItem("B", "tv"), MakeItem("C", "tv"), MakeItem("D", "movies") };

        var result = DownloadManagerStateExtensions.PaginateQueue(items, new QueryQueue(Start: 1, Limit: 1, Category: "tv"), 3);

        Assert.Single(result.Items);
        Assert.Equal("B", result.Items[0].Title);
        Assert.Equal(3, result.TotalItems);
    }
}
