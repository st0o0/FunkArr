using FunkArr.Download.Worker;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Download.Tests.Worker;

public sealed class DownloadWorkerFfmpegCrashTests
{
    private static readonly Guid _testId = Guid.NewGuid();

    private static readonly PersistedDownloadMedia _testMedia = new(
        "Test Video", "https://example.com/video.mp4", null,
        "ARD", 3600, 1_000_000, PersistedMediaType.Show);

    [Fact]
    public void Permanent_failure_sets_status_failed()
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "FFmpeg crashed: Segmentation fault"));

        Assert.Equal(WorkerStatus.Failed, state.Status);
        Assert.Equal("FFmpeg crashed: Segmentation fault", state.FailMessage);
    }

    [Fact]
    public void Permanent_failure_kind_is_default()
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "FFmpeg crashed"));

        Assert.Equal(FailureKind.Permanent, state.LastFailureKind);
    }

    [Fact]
    public void Transient_failure_allows_retry()
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "Connection reset", PersistedFailureKind.Transient));

        Assert.Equal(WorkerStatus.Failed, state.Status);
        Assert.Equal(FailureKind.Transient, state.LastFailureKind);
    }

    [Fact]
    public void Multiple_transient_failures_track_attempt_count()
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));

        for (var i = 1; i <= 4; i++)
        {
            state.Apply(new DownloadAttemptStarted(_testId, i));
            state.Apply(new DownloadFaulted(_testId, $"Transient error {i}", PersistedFailureKind.Transient));
        }

        Assert.Equal(4, state.Attempt);
        Assert.Equal(WorkerStatus.Failed, state.Status);
    }

    [Fact]
    public void Failure_after_empty_video_url_sets_correct_message()
    {
        var emptyUrlMedia = new PersistedDownloadMedia(
            "Test Video", "", null,
            "ARD", 3600, 1_000_000, PersistedMediaType.Show);
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, emptyUrlMedia));
        state.Apply(new DownloadFaulted(_testId, "Video URL is empty"));

        Assert.Equal(WorkerStatus.Failed, state.Status);
        Assert.Equal("Video URL is empty", state.FailMessage);
    }

    [Fact]
    public void Reinitialize_after_failure_resets_state()
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));
        state.Apply(new DownloadAttemptStarted(_testId, 1));
        state.Apply(new DownloadFaulted(_testId, "FFmpeg crashed"));

        Assert.Equal(WorkerStatus.Failed, state.Status);

        state.Apply(new DownloadInitialized(_testId, _testMedia));

        Assert.Equal(WorkerStatus.Initialized, state.Status);
        Assert.Null(state.FailMessage);
        Assert.Equal(0, state.Attempt);
    }
}
