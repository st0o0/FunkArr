using FunkArr.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadWorkerStateTests
{
    private static readonly Guid _testId = Guid.NewGuid();

    private static DownloadInitialized MakeInitialized() =>
        new(_testId, "Test Video", "https://example.com/video.mp4", "https://example.com/sub.srt",
            "ARD", 3600, 1_000_000, "tv");

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
        Assert.Equal("tv", state.Category);
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
            .Apply(new DownloadSucceeded(_testId, "/downloads/test.mkv", 120, 1234567890));

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
}
