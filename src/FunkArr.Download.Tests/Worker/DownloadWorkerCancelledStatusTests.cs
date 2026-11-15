using FunkArr.Download.Worker;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;
using FunkArr.Persistence.Events.Shared;
using Microsoft.Extensions.Time.Testing;

namespace FunkArr.Download.Tests.Worker;

public sealed class DownloadWorkerCancelledStatusTests
{
    private static readonly Guid _testId = Guid.NewGuid();
    private readonly FakeTimeProvider _timeProvider = new();

    private static readonly PersistedDownloadMedia _testMedia = new(
        "Test Video", "https://example.com/video.mp4", null,
        "ARD", 3600, 1_000_000, PersistedMediaType.Show);

    [Fact]
    public void ToRecordDownload_cancelled_message_produces_cancelled_status()
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Cancelled"));

        var record = state.ToRecordDownload(_testId, _timeProvider, 0, null);

        Assert.Equal(DownloadStatus.Cancelled, record.Completion.Status);
    }

    [Fact]
    public void ToRecordDownload_transient_failure_produces_failed_status()
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Connection refused", PersistedFailureKind.Transient));

        var record = state.ToRecordDownload(_testId, _timeProvider, 0, null);

        Assert.Equal(DownloadStatus.Failed, record.Completion.Status);
    }

    [Fact]
    public void ToRecordDownload_permanent_failure_produces_failed_status()
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Not found"));

        var record = state.ToRecordDownload(_testId, _timeProvider, 0, null);

        Assert.Equal(DownloadStatus.Failed, record.Completion.Status);
    }

    [Fact]
    public void ToRecordDownload_completed_produces_completed_status()
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadSucceeded(_testId, 120, 1234567890));

        var record = state.ToRecordDownload(_testId, _timeProvider, 120, null);

        Assert.Equal(DownloadStatus.Completed, record.Completion.Status);
    }
}
