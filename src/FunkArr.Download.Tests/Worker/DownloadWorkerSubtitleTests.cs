using FunkArr.Download.Worker;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Download.Tests.Worker;

public sealed class DownloadWorkerSubtitleTests
{
    private static readonly Guid _testId = Guid.NewGuid();

    [Fact]
    public void SubtitleMuxed_defaults_to_false()
    {
        var state = new DownloadWorkerState();

        Assert.False(state.SubtitleMuxed);
    }

    [Fact]
    public void SubtitleMuxed_false_when_subtitle_url_present_but_not_muxed()
    {
        var media = new PersistedDownloadMedia(
            "Test Video", "https://example.com/video.mp4", "https://example.com/sub.srt",
            "ARD", 3600, 1_000_000, PersistedMediaType.Show);
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, media));

        Assert.NotNull(state.Media!.SubtitleUrl);
        Assert.False(state.SubtitleMuxed);
    }

    [Fact]
    public void SubtitleMuxed_true_when_set_explicitly()
    {
        var media = new PersistedDownloadMedia(
            "Test Video", "https://example.com/video.mp4", "https://example.com/sub.srt",
            "ARD", 3600, 1_000_000, PersistedMediaType.Show);
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, media));

        state.SubtitleMuxed = true;

        Assert.True(state.SubtitleMuxed);
    }

    [Fact]
    public void SubtitleMuxed_false_when_no_subtitle_url()
    {
        var media = new PersistedDownloadMedia(
            "Test Video", "https://example.com/video.mp4", null,
            "ARD", 3600, 1_000_000, PersistedMediaType.Show);
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, media));

        Assert.Null(state.Media!.SubtitleUrl);
        Assert.False(state.SubtitleMuxed);
    }

    [Fact]
    public void SubtitleMuxed_resets_on_reinitialize()
    {
        var media = new PersistedDownloadMedia(
            "Test Video", "https://example.com/video.mp4", "https://example.com/sub.srt",
            "ARD", 3600, 1_000_000, PersistedMediaType.Show);
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, media));
        state.SubtitleMuxed = true;

        state.Apply(new DownloadInitialized(_testId, media));

        Assert.False(state.SubtitleMuxed);
    }
}
