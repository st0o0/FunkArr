using FunkArr.Download.Worker;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Download.Tests.Worker;

public sealed class DownloadWorkerRecoveryStateTests
{
    private static readonly Guid _testId = Guid.NewGuid();

    private static readonly PersistedDownloadMedia _testMedia = new(
        "Test Video", "https://example.com/video.mp4", "https://example.com/sub.srt",
        "ARD", 3600, 1_000_000, PersistedMediaType.Show);

    private static DownloadInitialized MakeInitialized() =>
        new(_testId, _testMedia);

    [Fact]
    public void Failed_worker_resets_to_initialized_on_reinit()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Connection refused", PersistedFailureKind.Transient));

        Assert.Equal(WorkerStatus.Failed, state.Status);

        state.Apply(MakeInitialized());

        Assert.Equal(WorkerStatus.Initialized, state.Status);
        Assert.Equal(DownloadPhase.Initialized, state.Phase);
        Assert.Null(state.FailMessage);
    }

    [Fact]
    public void Failed_worker_can_start_after_reinit()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Timeout", PersistedFailureKind.Transient));

        state.Apply(MakeInitialized());
        state.Apply(new DownloadAttemptStarted(_testId, 1));

        Assert.Equal(WorkerStatus.Downloading, state.Status);
        Assert.Equal(1, state.Attempt);
    }

    [Fact]
    public void Completed_worker_state_is_completed()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadSucceeded(_testId, 120, 1234567890));

        Assert.Equal(WorkerStatus.Completed, state.Status);
    }
}
