using FunkArr.Messages;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadWorkerRetryStateTests
{
    private static readonly Guid _testId = Guid.NewGuid();

    private static DownloadInitialized MakeInitialized() =>
        new(_testId, "Test Video", "https://example.com/video.mp4", "https://example.com/sub.srt",
            "ARD", 3600, 1_000_000, PersistedMediaType.Show);

    private static DownloadWorkerState FailedState() =>
        DownloadWorkerState.Empty
            .Apply(MakeInitialized())
            .Apply(new DownloadStarted(_testId))
            .Apply(new DownloadFaulted(_testId, "Connection refused"));

    [Fact]
    public void Re_initialize_after_failure_resets_status_to_initialized()
    {
        var state = FailedState().Apply(MakeInitialized());

        Assert.Equal(WorkerStatus.Initialized, state.Status);
    }

    [Fact]
    public void Re_initialize_after_failure_clears_fail_message()
    {
        var state = FailedState().Apply(MakeInitialized());

        Assert.Null(state.FailMessage);
    }

    [Fact]
    public void Re_initialize_after_failure_preserves_metadata()
    {
        var state = FailedState().Apply(MakeInitialized());

        Assert.Equal("Test Video", state.Title);
        Assert.Equal("https://example.com/video.mp4", state.VideoUrl);
        Assert.Equal("https://example.com/sub.srt", state.SubtitleUrl);
        Assert.Equal("ARD", state.Channel);
        Assert.Equal(3600, state.Duration);
        Assert.Equal(1_000_000L, state.Size);
        Assert.Equal(MediaType.Show, state.Category);
    }

    [Fact]
    public void Re_initialize_after_failure_resets_progress()
    {
        var state = FailedState() with
        {
            BytesDownloaded = 500_000,
            CurrentTimeUs = 1_800_000_000,
            Speed = 2.5,
        };

        state = state.Apply(MakeInitialized());

        Assert.Equal(0L, state.BytesDownloaded);
        Assert.Equal(0L, state.CurrentTimeUs);
        Assert.Equal(0.0, state.Speed);
    }

    [Fact]
    public void Re_initialized_worker_can_start_again()
    {
        var state = FailedState()
            .Apply(MakeInitialized())
            .Apply(new DownloadStarted(_testId));

        Assert.Equal(WorkerStatus.Downloading, state.Status);
    }

    [Fact]
    public void Re_initialized_worker_can_succeed_on_second_attempt()
    {
        var state = FailedState()
            .Apply(MakeInitialized())
            .Apply(new DownloadStarted(_testId))
            .Apply(new DownloadSucceeded(_testId, 120, 1234567890));

        Assert.Equal(WorkerStatus.Completed, state.Status);
    }

    [Fact]
    public void Re_initialized_worker_can_fail_again()
    {
        var state = FailedState()
            .Apply(MakeInitialized())
            .Apply(new DownloadStarted(_testId))
            .Apply(new DownloadFaulted(_testId, "Timeout"));

        Assert.Equal(WorkerStatus.Failed, state.Status);
        Assert.Equal("Timeout", state.FailMessage);
    }
}
