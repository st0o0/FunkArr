using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadManagerStateTests
{
    private static DownloadEnqueued Enqueue(Guid id, PersistedDownloadPriority priority = PersistedDownloadPriority.Normal, PersistedMediaType category = PersistedMediaType.Show)
        => new(id, priority, category);

    [Fact]
    public void Apply_Enqueued_adds_to_queue()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id));

        Assert.Single(state.Queued);
        Assert.Equal(id, state.Queued[0].Id);
        Assert.Equal(DownloadPriority.Normal, state.Queued[0].Priority);
        Assert.Empty(state.Dispatched);
    }

    [Fact]
    public void Apply_Dispatched_moves_from_queued_to_dispatched()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id))
            .Apply(new DownloadDispatched(id));

        Assert.Empty(state.Queued);
        Assert.True(state.Dispatched.ContainsKey(id));
    }

    [Fact]
    public void Apply_Dequeued_removes_from_dispatched()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id))
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
            .Apply(Enqueue(id))
            .Apply(new DownloadDequeued(id));

        Assert.Empty(state.Queued);
        Assert.Empty(state.Dispatched);
    }

    [Fact]
    public void ResetDispatched_moves_dispatched_to_queue()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id1))
            .Apply(Enqueue(id2))
            .Apply(new DownloadDispatched(id1))
            .ResetDispatched();

        Assert.Equal(2, state.Queued.Count);
        Assert.Contains(state.Queued, e => e.Id == id1);
        Assert.Contains(state.Queued, e => e.Id == id2);
        Assert.Empty(state.Dispatched);
    }

    [Fact]
    public void Contains_finds_queued_items()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id));

        Assert.True(state.Contains(id));
        Assert.False(state.Contains(Guid.NewGuid()));
    }

    [Fact]
    public void Contains_finds_dispatched_items()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id))
            .Apply(new DownloadDispatched(id));

        Assert.True(state.Contains(id));
    }

    [Fact]
    public void Queue_preserves_insertion_order_within_same_priority()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id1))
            .Apply(Enqueue(id2))
            .Apply(Enqueue(id3));

        Assert.Equal(3, state.Queued.Count);
        Assert.Equal(id1, state.Queued[0].Id);
        Assert.Equal(id2, state.Queued[1].Id);
        Assert.Equal(id3, state.Queued[2].Id);
    }

    [Fact]
    public void Multiple_dispatches_respected()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id1))
            .Apply(Enqueue(id2))
            .Apply(Enqueue(id3))
            .Apply(new DownloadDispatched(id1))
            .Apply(new DownloadDispatched(id2));

        Assert.Single(state.Queued);
        Assert.Equal(id3, state.Queued[0].Id);
        Assert.Equal(2, state.Dispatched.Count);
    }

    [Fact]
    public void Apply_Paused_sets_paused_true()
    {
        var state = DownloadManagerState.Empty
            .Apply(new DownloadsPaused());

        Assert.True(state.Paused);
    }

    [Fact]
    public void Apply_Resumed_sets_paused_false()
    {
        var state = DownloadManagerState.Empty
            .Apply(new DownloadsPaused())
            .Apply(new DownloadsResumed());

        Assert.False(state.Paused);
    }

    [Fact]
    public void Default_state_has_schedule_enabled_and_not_paused()
    {
        Assert.False(DownloadManagerState.Empty.Paused);
        Assert.True(DownloadManagerState.Empty.ScheduleEnabled);
        Assert.Null(DownloadManagerState.Empty.NextWindow);
    }

    [Fact]
    public void GetPage_returns_all_ids_when_limit_zero()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id1))
            .Apply(Enqueue(id2))
            .Apply(Enqueue(id3));

        var (pageIds, totalItems) = state.GetPage(new QueryQueue());

        Assert.Equal(3, pageIds.Length);
        Assert.Equal(3, totalItems);
    }

    [Fact]
    public void GetPage_applies_start_and_limit()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var id4 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id1))
            .Apply(Enqueue(id2))
            .Apply(Enqueue(id3))
            .Apply(Enqueue(id4));

        var (pageIds, totalItems) = state.GetPage(new QueryQueue(Start: 1, Limit: 2));

        Assert.Equal(2, pageIds.Length);
        Assert.Equal(id2, pageIds[0]);
        Assert.Equal(id3, pageIds[1]);
        Assert.Equal(4, totalItems);
    }

    [Fact]
    public void GetPage_filters_by_category()
    {
        var show1 = Guid.NewGuid();
        var movie1 = Guid.NewGuid();
        var show2 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(show1, category: PersistedMediaType.Show))
            .Apply(Enqueue(movie1, category: PersistedMediaType.Movie))
            .Apply(Enqueue(show2, category: PersistedMediaType.Show));

        var (pageIds, totalItems) = state.GetPage(new QueryQueue(Category: MediaType.Show));

        Assert.Equal(2, pageIds.Length);
        Assert.Equal(2, totalItems);
        Assert.Contains(show1, pageIds);
        Assert.Contains(show2, pageIds);
    }

    [Fact]
    public void GetPage_category_filter_with_pagination()
    {
        var show1 = Guid.NewGuid();
        var show2 = Guid.NewGuid();
        var show3 = Guid.NewGuid();
        var movie1 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(show1, category: PersistedMediaType.Show))
            .Apply(Enqueue(show2, category: PersistedMediaType.Show))
            .Apply(Enqueue(show3, category: PersistedMediaType.Show))
            .Apply(Enqueue(movie1, category: PersistedMediaType.Movie));

        var (pageIds, totalItems) = state.GetPage(new QueryQueue(Start: 1, Limit: 1, Category: MediaType.Show));

        Assert.Single(pageIds);
        Assert.Equal(show2, pageIds[0]);
        Assert.Equal(3, totalItems);
    }

    [Fact]
    public void GetPage_includes_dispatched_ids_first()
    {
        var dispatched1 = Guid.NewGuid();
        var queued1 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(dispatched1))
            .Apply(Enqueue(queued1))
            .Apply(new DownloadDispatched(dispatched1));

        var (pageIds, totalItems) = state.GetPage(new QueryQueue());

        Assert.Equal(2, pageIds.Length);
        Assert.Equal(2, totalItems);
        Assert.Equal(dispatched1, pageIds[0]);
        Assert.Equal(queued1, pageIds[1]);
    }

    [Fact]
    public void GetPage_empty_queue_returns_empty()
    {
        var (pageIds, totalItems) = DownloadManagerState.Empty.GetPage(new QueryQueue());

        Assert.Empty(pageIds);
        Assert.Equal(0, totalItems);
    }

    [Fact]
    public void Enqueue_with_priority_inserts_in_correct_bucket()
    {
        var high1 = Guid.NewGuid();
        var normal1 = Guid.NewGuid();
        var low1 = Guid.NewGuid();
        var high2 = Guid.NewGuid();

        var state = DownloadManagerState.Empty
            .Apply(Enqueue(normal1))
            .Apply(Enqueue(high1, PersistedDownloadPriority.High))
            .Apply(Enqueue(low1, PersistedDownloadPriority.Low))
            .Apply(Enqueue(high2, PersistedDownloadPriority.High));

        Assert.Equal(4, state.Queued.Count);
        Assert.Equal(high1, state.Queued[0].Id);
        Assert.Equal(high2, state.Queued[1].Id);
        Assert.Equal(normal1, state.Queued[2].Id);
        Assert.Equal(low1, state.Queued[3].Id);
    }

    [Fact]
    public void Move_within_bucket_repositions_item()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id1))
            .Apply(Enqueue(id2))
            .Apply(Enqueue(id3))
            .Apply(new DownloadMoved(id3, 0));

        Assert.Equal(id3, state.Queued[0].Id);
        Assert.Equal(id1, state.Queued[1].Id);
        Assert.Equal(id2, state.Queued[2].Id);
    }

    [Fact]
    public void Move_clamps_to_bucket_boundary()
    {
        var high = Guid.NewGuid();
        var normal1 = Guid.NewGuid();
        var normal2 = Guid.NewGuid();

        var state = DownloadManagerState.Empty
            .Apply(Enqueue(high, PersistedDownloadPriority.High))
            .Apply(Enqueue(normal1))
            .Apply(Enqueue(normal2))
            .Apply(new DownloadMoved(normal2, 0));

        Assert.Equal(high, state.Queued[0].Id);
        Assert.Equal(normal2, state.Queued[1].Id);
        Assert.Equal(normal1, state.Queued[2].Id);
    }

    [Fact]
    public void Move_clamps_to_bucket_end()
    {
        var normal1 = Guid.NewGuid();
        var normal2 = Guid.NewGuid();
        var low = Guid.NewGuid();

        var state = DownloadManagerState.Empty
            .Apply(Enqueue(normal1))
            .Apply(Enqueue(normal2))
            .Apply(Enqueue(low, PersistedDownloadPriority.Low))
            .Apply(new DownloadMoved(normal1, 99));

        Assert.Equal(normal2, state.Queued[0].Id);
        Assert.Equal(normal1, state.Queued[1].Id);
        Assert.Equal(low, state.Queued[2].Id);
    }

    [Fact]
    public void Swap_exchanges_positions_within_same_bucket()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id1))
            .Apply(Enqueue(id2))
            .Apply(Enqueue(id3))
            .Apply(new DownloadSwapped(id1, id3));

        Assert.Equal(id3, state.Queued[0].Id);
        Assert.Equal(id2, state.Queued[1].Id);
        Assert.Equal(id1, state.Queued[2].Id);
    }

    [Fact]
    public void PriorityChanged_moves_to_end_of_target_bucket()
    {
        var high1 = Guid.NewGuid();
        var normal1 = Guid.NewGuid();
        var normal2 = Guid.NewGuid();

        var state = DownloadManagerState.Empty
            .Apply(Enqueue(high1, PersistedDownloadPriority.High))
            .Apply(Enqueue(normal1))
            .Apply(Enqueue(normal2))
            .Apply(new DownloadPriorityChanged(normal2, PersistedDownloadPriority.High));

        Assert.Equal(3, state.Queued.Count);
        Assert.Equal(high1, state.Queued[0].Id);
        Assert.Equal(normal2, state.Queued[1].Id);
        Assert.Equal(DownloadPriority.High, state.Queued[1].Priority);
        Assert.Equal(normal1, state.Queued[2].Id);
    }

    [Fact]
    public void Dispatched_preserves_priority()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(id, PersistedDownloadPriority.High))
            .Apply(new DownloadDispatched(id));

        Assert.True(state.Dispatched.ContainsKey(id));
        Assert.Equal(DownloadPriority.High, state.Dispatched[id].Priority);
    }

    [Fact]
    public void ResetDispatched_preserves_priority()
    {
        var highId = Guid.NewGuid();
        var normalId = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(Enqueue(highId, PersistedDownloadPriority.High))
            .Apply(Enqueue(normalId))
            .Apply(new DownloadDispatched(highId))
            .ResetDispatched();

        Assert.Equal(2, state.Queued.Count);
        var highEntry = state.Queued.First(e => e.Id == highId);
        Assert.Equal(DownloadPriority.High, highEntry.Priority);
        Assert.Equal(highId, state.Queued[0].Id);
        Assert.Equal(normalId, state.Queued[1].Id);
    }
}
