using FunkArr.Download;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadManagerStateTests
{
    private static DownloadQueued MakeQueued(Guid id, string title = "Test") =>
        new(id, title, "https://example.com/video.mp4", null, "ARD", 3600, 1_000_000, "tv");

    [Fact]
    public void Apply_DownloadQueued_adds_to_queue()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty.Apply(MakeQueued(id));

        Assert.Single(state.Queue);
        Assert.Equal(id, state.Queue[0].DownloadId);
        Assert.Equal(DownloadStatus.Queued, state.Queue[0].Status);
    }

    [Fact]
    public void Apply_StatusChanged_to_processing()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id))
            .Apply(new DownloadStatusChanged(id, (int)DownloadStatus.Processing, null, 0, null, 0));

        Assert.Single(state.Queue);
        Assert.Equal(DownloadStatus.Processing, state.Queue[0].Status);
    }

    [Fact]
    public void Apply_StatusChanged_to_completed_moves_to_history()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id))
            .Apply(new DownloadStatusChanged(id, (int)DownloadStatus.Completed,
                "/downloads/test.mkv", 120, null, 1234567890));

        Assert.Empty(state.Queue);
        Assert.Single(state.History);
        Assert.Equal(DownloadStatus.Completed, state.History[0].Status);
        Assert.Equal("/downloads/test.mkv", state.History[0].FilePath);
    }

    [Fact]
    public void Apply_StatusChanged_to_failed_moves_to_history()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id))
            .Apply(new DownloadStatusChanged(id, (int)DownloadStatus.Failed,
                null, 0, "Connection refused", 1234567890));

        Assert.Empty(state.Queue);
        Assert.Single(state.History);
        Assert.Equal(DownloadStatus.Failed, state.History[0].Status);
        Assert.Equal("Connection refused", state.History[0].FailMessage);
    }

    [Fact]
    public void Apply_DownloadRemoved_removes_from_queue()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id))
            .Apply(new DownloadRemoved(id));

        Assert.Empty(state.Queue);
    }

    [Fact]
    public void Apply_DownloadRemoved_removes_from_history()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id))
            .Apply(new DownloadStatusChanged(id, (int)DownloadStatus.Completed,
                "/downloads/test.mkv", 120, null, 1234567890))
            .Apply(new DownloadRemoved(id));

        Assert.Empty(state.History);
    }

    [Fact]
    public void UpdateProgress_updates_in_memory()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id))
            .UpdateProgress(id, 500_000, 1_800_000_000, 1.5);

        Assert.Equal(500_000L, state.Queue[0].BytesDownloaded);
        Assert.Equal(1_800_000_000L, state.Queue[0].CurrentTimeUs);
        Assert.Equal(1.5, state.Queue[0].Speed);
    }

    [Fact]
    public void RequeueProcessing_resets_processing_items()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id))
            .Apply(new DownloadStatusChanged(id, (int)DownloadStatus.Processing, null, 0, null, 0))
            .UpdateProgress(id, 500_000, 1_800_000_000, 1.5)
            .RequeueProcessing();

        Assert.Equal(DownloadStatus.Queued, state.Queue[0].Status);
        Assert.Equal(0L, state.Queue[0].BytesDownloaded);
        Assert.Equal(0.0, state.Queue[0].Speed);
    }

    [Fact]
    public void ActiveCount_counts_processing_items()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id1))
            .Apply(MakeQueued(id2))
            .Apply(new DownloadStatusChanged(id1, (int)DownloadStatus.Processing, null, 0, null, 0));

        Assert.Equal(1, state.ActiveCount());
    }

    [Fact]
    public void NextQueued_returns_first_queued()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id1, "First"))
            .Apply(MakeQueued(id2, "Second"))
            .Apply(new DownloadStatusChanged(id1, (int)DownloadStatus.Processing, null, 0, null, 0));

        var next = state.NextQueued();
        Assert.NotNull(next);
        Assert.Equal(id2, next.DownloadId);
    }

    [Fact]
    public void ToQueueResult_maps_all_queue_items()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty.Apply(MakeQueued(id));
        var result = state.ToQueueResult();

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalSlots);
        Assert.Equal(id, result.Items[0].DownloadId);
    }

    [Fact]
    public void ToHistoryResult_maps_all_history_items()
    {
        var id = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(MakeQueued(id))
            .Apply(new DownloadStatusChanged(id, (int)DownloadStatus.Completed,
                "/downloads/test.mkv", 120, null, 1234567890));

        var result = state.ToHistoryResult();
        Assert.Single(result.Items);
        Assert.Equal(id, result.Items[0].DownloadId);
    }
}
