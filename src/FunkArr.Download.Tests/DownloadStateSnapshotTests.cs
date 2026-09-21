using FunkArr.Download;
using FunkArr.Messages;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadStateSnapshotTests
{
    [Fact]
    public void DownloadManagerState_SnapshotRoundTrip()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var state = DownloadManagerState.Empty
            .Apply(new DownloadEnqueued(id1))
            .Apply(new DownloadEnqueued(id2))
            .Apply(new DownloadDispatched(id1));

        var snapshot = state.GetSnapshot();
        var restored = DownloadManagerState.FromSnapshot(snapshot);

        Assert.Single(restored.Queued);
        Assert.Contains(id2, restored.Queued);
        Assert.Single(restored.Dispatched);
        Assert.Contains(id1, restored.Dispatched);
    }

    [Fact]
    public void DownloadHistoryManagerState_SnapshotRoundTrip()
    {
        var id = Guid.NewGuid();
        var state = DownloadHistoryManagerState.Empty
            .Apply(new HistoryRecorded(id, "Test", MediaType.Show, 1000, 1, null, null, 60, 123456));

        var snapshot = state.GetSnapshot();
        var restored = DownloadHistoryManagerState.FromSnapshot(snapshot);

        Assert.Single(restored.Records);
        Assert.Equal(id, restored.Records[0].DownloadId);
        Assert.Equal("Test", restored.Records[0].Title);
    }

    [Fact]
    public void DownloadWorkerState_SnapshotRoundTrip()
    {
        var id = Guid.NewGuid();
        var state = DownloadWorkerState.Empty
            .Apply(new DownloadInitialized(id, "Test", "http://video", "http://sub", "ARD", 3600, 1000, MediaType.Show))
            .Apply(new DownloadStarted(id));

        var snapshot = state.GetSnapshot();
        var restored = DownloadWorkerState.FromSnapshot(snapshot);

        Assert.Equal("Test", restored.Title);
        Assert.Equal("http://video", restored.VideoUrl);
        Assert.Equal(WorkerStatus.Downloading, restored.Status);
        Assert.Equal(0, restored.BytesDownloaded);
        Assert.Equal(0.0, restored.Speed);
    }

    [Fact]
    public void DownloadManagerState_EmptySnapshotRoundTrip()
    {
        var snapshot = DownloadManagerState.Empty.GetSnapshot();
        var restored = DownloadManagerState.FromSnapshot(snapshot);

        Assert.Empty(restored.Queued);
        Assert.Empty(restored.Dispatched);
    }
}
