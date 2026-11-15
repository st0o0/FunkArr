using FunkArr.Download.Worker;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Download.Tests.Worker;

public sealed class DownloadWorkerJournalReplayTests
{
    private static readonly Guid _testId = Guid.NewGuid();

    private static readonly PersistedDownloadMedia _testMedia = new(
        "Tatort.S2026E01.GERMAN.1080p.WEB.h264", "https://example.com/video.mp4",
        "https://example.com/sub.srt", "ARD", 3600, 1_000_000, PersistedMediaType.Show);

    [Fact]
    public void Full_journal_replay_initialized()
    {
        var state = DownloadWorkerState.Empty;
        state.Apply(new DownloadInitialized(_testId, _testMedia));

        Assert.True(state.IsInitialized);
        Assert.Equal(WorkerStatus.Initialized, state.Status);
        Assert.Equal(DownloadPhase.Initialized, state.Phase);
        Assert.Equal("Tatort.S2026E01.GERMAN.1080p.WEB.h264", state.Media!.Title);
    }

    [Fact]
    public void Full_journal_replay_completed()
    {
        var state = DownloadWorkerState.Empty;
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadSucceeded(_testId, 120, 1234567890));

        Assert.Equal(WorkerStatus.Completed, state.Status);
        Assert.Equal(DownloadPhase.Completed, state.Phase);
        Assert.Equal(1, state.Attempt);
    }

    [Fact]
    public void Full_journal_replay_failed()
    {
        var state = DownloadWorkerState.Empty;
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Connection refused", PersistedFailureKind.Transient));

        Assert.Equal(WorkerStatus.Failed, state.Status);
        Assert.Equal(DownloadPhase.Failed, state.Phase);
        Assert.Equal("Connection refused", state.FailMessage);
        Assert.Equal(FailureKind.Transient, state.LastFailureKind);
    }

    [Fact]
    public void Transient_phase_resets_to_initialized_after_recovery()
    {
        var state = DownloadWorkerState.Empty;
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));

        Assert.Equal(WorkerStatus.Downloading, state.Status);
        Assert.True(state.Phase.IsTransient());

        state.ResetAfterRecovery();

        Assert.Equal(WorkerStatus.Initialized, state.Status);
        Assert.Equal(DownloadPhase.Initialized, state.Phase);
        Assert.True(state.IsInitialized);
    }

    [Fact]
    public void Completed_phase_not_reset_after_recovery()
    {
        var state = DownloadWorkerState.Empty;
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadSucceeded(_testId, 120, 1234567890));

        Assert.False(state.Phase.IsTransient());

        Assert.Equal(WorkerStatus.Completed, state.Status);
    }

    [Fact]
    public void Multi_attempt_replay_tracks_attempt_count()
    {
        var state = DownloadWorkerState.Empty;
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Transient error", PersistedFailureKind.Transient));
        state.Apply(new DownloadAttemptStarted(_testId, 2));
        state.Apply(new DownloadFaulted(_testId, "Transient error again", PersistedFailureKind.Transient));
        state.Apply(new DownloadAttemptStarted(_testId, 3));
        state.Apply(new DownloadSucceeded(_testId, 300, 1234567890));

        Assert.Equal(WorkerStatus.Completed, state.Status);
        Assert.Equal(3, state.Attempt);
    }

    [Fact]
    public void Failed_state_not_transient_stays_failed_after_recovery()
    {
        var state = DownloadWorkerState.Empty;
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Permanent error", PersistedFailureKind.Permanent));

        Assert.False(state.Phase.IsTransient());
    }
}
