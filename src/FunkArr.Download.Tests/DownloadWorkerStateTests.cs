using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadWorkerStateTests
{
    private static readonly Guid _testId = Guid.NewGuid();

    private static DownloadInitialized MakeInitialized() =>
        new(_testId, "Test Video", "https://example.com/video.mp4", "https://example.com/sub.srt",
            "ARD", 3600, 1_000_000, PersistedMediaType.Show);

    private static DownloadInitialized MakeInitializedWithRoute() =>
        new(_testId, "Test Video", "https://example.com/video.mp4", "https://example.com/sub.srt",
            "ARD", 3600, 1_000_000, PersistedMediaType.Show, "ProxyRoute", "http://proxy:8080");

    [Fact]
    public void Empty_is_not_initialized()
    {
        Assert.False(DownloadWorkerState.Empty.IsInitialized);
    }

    [Fact]
    public void Apply_Initialized_sets_metadata()
    {
        var state = DownloadWorkerState.Empty.Apply(MakeInitialized());

        Assert.True(state.IsInitialized);
        Assert.Equal("Test Video", state.Title);
        Assert.Equal("https://example.com/video.mp4", state.VideoUrl);
        Assert.Equal("https://example.com/sub.srt", state.SubtitleUrl);
        Assert.Equal("ARD", state.Channel);
        Assert.Equal(3600, state.Duration);
        Assert.Equal(1_000_000L, state.Size);
        Assert.Equal(MediaType.Show, state.Category);
        Assert.Equal(WorkerStatus.Initialized, state.Status);
    }

    [Fact]
    public void Apply_Started_sets_downloading()
    {
        var state = DownloadWorkerState.Empty
            .Apply(MakeInitialized())
            .Apply(new DownloadStarted(_testId));

        Assert.Equal(WorkerStatus.Downloading, state.Status);
    }

    [Fact]
    public void Apply_Succeeded_sets_completed()
    {
        var state = DownloadWorkerState.Empty
            .Apply(MakeInitialized())
            .Apply(new DownloadStarted(_testId))
            .Apply(new DownloadSucceeded(_testId, 120, 1234567890));

        Assert.Equal(WorkerStatus.Completed, state.Status);
    }

    [Fact]
    public void Apply_Faulted_sets_failed_with_message()
    {
        var state = DownloadWorkerState.Empty
            .Apply(MakeInitialized())
            .Apply(new DownloadStarted(_testId))
            .Apply(new DownloadFaulted(_testId, "Connection refused"));

        Assert.Equal(WorkerStatus.Failed, state.Status);
        Assert.Equal("Connection refused", state.FailMessage);
    }

    [Fact]
    public void Re_initialize_resets_to_initialized()
    {
        var state = DownloadWorkerState.Empty
            .Apply(MakeInitialized())
            .Apply(new DownloadStarted(_testId))
            .Apply(new DownloadFaulted(_testId, "Error"))
            .Apply(MakeInitialized());

        Assert.Equal(WorkerStatus.Initialized, state.Status);
        Assert.Null(state.FailMessage);
    }

    [Fact]
    public void Empty_has_zero_progress()
    {
        var state = DownloadWorkerState.Empty;

        Assert.Equal(0L, state.BytesDownloaded);
        Assert.Equal(0L, state.CurrentTimeUs);
        Assert.Equal(0.0, state.Speed);
    }

    [Fact]
    public void Initialize_resets_progress()
    {
        var state = DownloadWorkerState.Empty with
        {
            BytesDownloaded = 500_000,
            CurrentTimeUs = 1_000_000,
            Speed = 1.5,
        };

        state = state.Apply(MakeInitialized());

        Assert.Equal(0L, state.BytesDownloaded);
        Assert.Equal(0L, state.CurrentTimeUs);
        Assert.Equal(0.0, state.Speed);
    }

    [Fact]
    public void Apply_Initialized_sets_route_info()
    {
        var state = DownloadWorkerState.Empty.Apply(MakeInitializedWithRoute());
        Assert.Equal("ProxyRoute", state.RouteName);
        Assert.Equal("http://proxy:8080", state.ProxyUrl);
    }

    [Fact]
    public void Apply_Initialized_default_route()
    {
        var state = DownloadWorkerState.Empty.Apply(MakeInitialized());
        Assert.Equal("Direct", state.RouteName);
        Assert.Null(state.ProxyUrl);
    }

    [Fact]
    public void Apply_Initialized_sets_phase_initialized()
    {
        var state = DownloadWorkerState.Empty.Apply(MakeInitialized());
        Assert.Equal(DownloadPhase.Initialized, state.Phase);
        Assert.Equal(0, state.Attempt);
    }

    [Fact]
    public void Apply_AttemptStarted_sets_attempt_and_phase()
    {
        var state = DownloadWorkerState.Empty
            .Apply(MakeInitialized())
            .Apply(new DownloadAttemptStarted(_testId, 1));

        Assert.Equal(1, state.Attempt);
        Assert.Equal(WorkerStatus.Downloading, state.Status);
        Assert.Equal(DownloadPhase.VideoDownload, state.Phase);
    }

    [Fact]
    public void Apply_AttemptStarted_resets_progress()
    {
        var state = DownloadWorkerState.Empty
            .Apply(MakeInitialized())
            .Apply(new DownloadAttemptStarted(_testId, 1));
        state = state with { BytesDownloaded = 500_000, CurrentTimeUs = 1_000_000, Speed = 1.5 };
        state = state.Apply(new DownloadAttemptStarted(_testId, 2));

        Assert.Equal(2, state.Attempt);
        Assert.Equal(0L, state.BytesDownloaded);
        Assert.Equal(0L, state.CurrentTimeUs);
        Assert.Equal(0.0, state.Speed);
    }

    [Fact]
    public void Apply_Faulted_sets_failure_kind()
    {
        var state = DownloadWorkerState.Empty
            .Apply(MakeInitialized())
            .Apply(new DownloadAttemptStarted(_testId, 1))
            .Apply(new DownloadFaulted(_testId, "Server returned 503", PersistedFailureKind.Transient));

        Assert.Equal(WorkerStatus.Failed, state.Status);
        Assert.Equal(FailureKind.Transient, state.LastFailureKind);
    }

    [Fact]
    public void Apply_Faulted_defaults_to_permanent()
    {
        var state = DownloadWorkerState.Empty
            .Apply(MakeInitialized())
            .Apply(new DownloadFaulted(_testId, "Old event without kind"));

        Assert.Equal(FailureKind.Permanent, state.LastFailureKind);
    }

    [Fact]
    public void Transient_phase_is_detected()
    {
        Assert.True(DownloadPhase.VideoDownload.IsTransient());
        Assert.True(DownloadPhase.Remuxing.IsTransient());
        Assert.True(DownloadPhase.Moving.IsTransient());
        Assert.True(DownloadPhase.SubtitleDownload.IsTransient());
        Assert.False(DownloadPhase.Initialized.IsTransient());
        Assert.False(DownloadPhase.Completed.IsTransient());
        Assert.False(DownloadPhase.Failed.IsTransient());
    }
}
