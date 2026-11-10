using FunkArr.Download.Worker;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Download.Tests.Worker;

public sealed class DownloadWorkerStateTests
{
    private static readonly Guid _testId = Guid.NewGuid();

    private static readonly PersistedDownloadMedia _testMedia = new(
        "Test Video", "https://example.com/video.mp4", "https://example.com/sub.srt",
        "ARD", 3600, 1_000_000, PersistedMediaType.Show);

    private static DownloadInitialized MakeInitialized() =>
        new(_testId, _testMedia);

    [Fact]
    public void Empty_is_not_initialized()
    {
        Assert.False(new DownloadWorkerState().IsInitialized);
    }

    [Fact]
    public void Apply_Initialized_sets_metadata()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());

        Assert.True(state.IsInitialized);
        Assert.NotNull(state.Media);
        Assert.Equal("Test Video", state.Media.Title);
        Assert.Equal("https://example.com/video.mp4", state.Media.VideoUrl);
        Assert.Equal("https://example.com/sub.srt", state.Media.SubtitleUrl);
        Assert.Equal("ARD", state.Media.Channel);
        Assert.Equal(3600, state.Media.Duration);
        Assert.Equal(1_000_000L, state.Media.Size);
        Assert.Equal(MediaType.Show, state.Media.Category);
        Assert.Equal(WorkerStatus.Initialized, state.Status);
    }

    [Fact]
    public void Apply_Started_sets_downloading()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadStarted(_testId));

        Assert.Equal(WorkerStatus.Downloading, state.Status);
    }

    [Fact]
    public void Apply_Succeeded_sets_completed()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadStarted(_testId));
        state.Apply(new DownloadSucceeded(_testId, 120, 1234567890));

        Assert.Equal(WorkerStatus.Completed, state.Status);
    }

    [Fact]
    public void Apply_Faulted_sets_failed_with_message()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadStarted(_testId));
        state.Apply(new DownloadFaulted(_testId, "Connection refused"));

        Assert.Equal(WorkerStatus.Failed, state.Status);
        Assert.Equal("Connection refused", state.FailMessage);
    }

    [Fact]
    public void Re_initialize_resets_to_initialized()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadStarted(_testId));
        state.Apply(new DownloadFaulted(_testId, "Error"));
        state.Apply(MakeInitialized());

        Assert.Equal(WorkerStatus.Initialized, state.Status);
        Assert.Null(state.FailMessage);
    }

    [Fact]
    public void Empty_has_zero_progress()
    {
        var state = new DownloadWorkerState();

        Assert.Equal(0L, state.Progress.BytesDownloaded);
        Assert.Equal(0L, state.Progress.CurrentTimeUs);
        Assert.Equal(0.0, state.Progress.Speed);
    }

    [Fact]
    public void Initialize_resets_progress()
    {
        var state = new DownloadWorkerState();
        state.Apply(new WorkerProgress(new DownloadProgress(500_000, 1_000_000, 1.5), DownloadPhase.VideoDownload, 500_000));

        state.Apply(MakeInitialized());

        Assert.Equal(0L, state.Progress.BytesDownloaded);
        Assert.Equal(0L, state.Progress.CurrentTimeUs);
        Assert.Equal(0.0, state.Progress.Speed);
    }

    [Fact]
    public void Apply_Initialized_sets_phase_initialized()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        Assert.Equal(DownloadPhase.Initialized, state.Phase);
        Assert.Equal(0, state.Attempt);
    }

    [Fact]
    public void Apply_AttemptStarted_sets_attempt_and_phase()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadAttemptStarted(_testId, 1));

        Assert.Equal(1, state.Attempt);
        Assert.Equal(WorkerStatus.Downloading, state.Status);
        Assert.Equal(DownloadPhase.VideoDownload, state.Phase);
    }

    [Fact]
    public void Apply_AttemptStarted_resets_progress()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new WorkerProgress(new DownloadProgress(500_000, 1_000_000, 1.5), DownloadPhase.VideoDownload, 500_000));
        state.Apply(new DownloadAttemptStarted(_testId, 2));

        Assert.Equal(2, state.Attempt);
        Assert.Equal(0L, state.Progress.BytesDownloaded);
        Assert.Equal(0L, state.Progress.CurrentTimeUs);
        Assert.Equal(0.0, state.Progress.Speed);
    }

    [Fact]
    public void Apply_Faulted_sets_failure_kind()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Server returned 503", PersistedFailureKind.Transient));

        Assert.Equal(WorkerStatus.Failed, state.Status);
        Assert.Equal(FailureKind.Transient, state.LastFailureKind);
    }

    [Fact]
    public void Apply_Faulted_defaults_to_permanent()
    {
        var state = new DownloadWorkerState();
        state.Apply(MakeInitialized());
        state.Apply(new DownloadFaulted(_testId, "Old event without kind"));

        Assert.Equal(FailureKind.Permanent, state.LastFailureKind);
    }

    [Fact]
    public void Transient_phase_is_detected()
    {
        Assert.True(DownloadPhase.VideoDownload.IsTransient());
        Assert.True(DownloadPhase.Remuxing.IsTransient());
        Assert.True(DownloadPhase.Moving.IsTransient());
        Assert.False(DownloadPhase.Initialized.IsTransient());
        Assert.False(DownloadPhase.Completed.IsTransient());
        Assert.False(DownloadPhase.Failed.IsTransient());
    }
}
