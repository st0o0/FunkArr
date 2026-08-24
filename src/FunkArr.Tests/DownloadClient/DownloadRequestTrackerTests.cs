using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;
using Akka.Persistence;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Tracker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.DownloadClient;

public sealed class DownloadRequestActorTests : Akka.Hosting.TestKit.TestKit
{
    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.AddHocon(
            """
            akka.persistence.journal.plugin = "akka.persistence.journal.inmem"
            akka.persistence.snapshot-store.plugin = "akka.persistence.snapshot-store.inmem"
            """,
            HoconAddMode.Prepend);
    }

    [Fact]
    public async Task TrackDownload_ThenQueryStatus_ReturnsQueuedStatus()
    {
        var tracker = Sys.ActorOf(Props.Create<DownloadRequestActor>(), "tracker-1");
        var now = DateTimeOffset.UtcNow;

        tracker.Tell(new DownloadRequestActor.TrackDownload("tracker-1", "Test Video", "https://example.com/v.mp4", null, now));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.QueryStatus("tracker-1"));
        var status = await ExpectMsgAsync<DownloadRequestActor.DownloadStatus>(TimeSpan.FromSeconds(5));

        Assert.Equal("tracker-1", status.NzoId);
        Assert.Equal("Test Video", status.Title);
        Assert.Equal("Queued", status.Status);
        Assert.Equal(now, status.EnqueuedAt);
    }

    [Fact]
    public async Task ReportProgress_UpdatesStatus()
    {
        var tracker = Sys.ActorOf(Props.Create<DownloadRequestActor>(), "tracker-2");
        var now = DateTimeOffset.UtcNow;

        tracker.Tell(new DownloadRequestActor.TrackDownload("tracker-2", "Test", "https://example.com/v.mp4", null, now));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.ReportProgress("tracker-2", "Downloading"));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.QueryStatus("tracker-2"));
        var status = await ExpectMsgAsync<DownloadRequestActor.DownloadStatus>(TimeSpan.FromSeconds(5));

        Assert.Equal("Downloading", status.Status);
    }

    [Fact]
    public async Task CompleteDownload_MarksCompletedInHistory()
    {
        var tracker = Sys.ActorOf(Props.Create<DownloadRequestActor>(), "tracker-3");
        var now = DateTimeOffset.UtcNow;

        tracker.Tell(new DownloadRequestActor.TrackDownload("tracker-3", "Done Video", "https://example.com/v.mp4", null, now));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.CompleteDownload("tracker-3", "/out/video.mkv"));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.QueryHistory("tracker-3"));
        var history = await ExpectMsgAsync<DownloadRequestActor.DownloadHistoryEntry>(TimeSpan.FromSeconds(5));

        Assert.Equal("tracker-3", history.NzoId);
        Assert.Equal("Done Video", history.Title);
        Assert.Equal("Completed", history.Status);
        Assert.Equal("/out/video.mkv", history.OutputPath);
        Assert.NotNull(history.CompletedAt);
        Assert.Null(history.ErrorMessage);
    }

    [Fact]
    public async Task FailDownload_MarksFailedWithError()
    {
        var tracker = Sys.ActorOf(Props.Create<DownloadRequestActor>(), "tracker-4");
        var now = DateTimeOffset.UtcNow;

        tracker.Tell(new DownloadRequestActor.TrackDownload("tracker-4", "Fail Video", "https://example.com/v.mp4", null, now));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.FailDownload("tracker-4", "Connection timeout"));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.QueryHistory("tracker-4"));
        var history = await ExpectMsgAsync<DownloadRequestActor.DownloadHistoryEntry>(TimeSpan.FromSeconds(5));

        Assert.Equal("Failed", history.Status);
        Assert.Equal("Connection timeout", history.ErrorMessage);
        Assert.NotNull(history.CompletedAt);
    }

    [Fact]
    public async Task TrackDownload_WithCategory_PreservesCategoryInStatus()
    {
        var tracker = Sys.ActorOf(Props.Create<DownloadRequestActor>(), "tracker-5");
        var now = DateTimeOffset.UtcNow;

        tracker.Tell(new DownloadRequestActor.TrackDownload("tracker-5", "Categorized Video", "https://example.com/v.mp4", "tv", now));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.QueryStatus("tracker-5"));
        var status = await ExpectMsgAsync<DownloadRequestActor.DownloadStatus>(TimeSpan.FromSeconds(5));

        Assert.Equal("tv", status.Category);
    }

    [Fact]
    public async Task TrackDownload_WithoutCategory_HasNullCategory()
    {
        var tracker = Sys.ActorOf(Props.Create<DownloadRequestActor>(), "tracker-6");
        var now = DateTimeOffset.UtcNow;

        tracker.Tell(new DownloadRequestActor.TrackDownload("tracker-6", "Uncategorized Video", "https://example.com/v.mp4", null, now));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.QueryStatus("tracker-6"));
        var status = await ExpectMsgAsync<DownloadRequestActor.DownloadStatus>(TimeSpan.FromSeconds(5));

        Assert.Null(status.Category);
    }

    [Fact]
    public async Task CompleteDownload_WithCategory_PreservesCategoryInHistory()
    {
        var tracker = Sys.ActorOf(Props.Create<DownloadRequestActor>(), "tracker-7");
        var now = DateTimeOffset.UtcNow;

        tracker.Tell(new DownloadRequestActor.TrackDownload("tracker-7", "Cat Video", "https://example.com/v.mp4", "movies", now));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.CompleteDownload("tracker-7", "/out/video.mkv"));
        await Task.Delay(100);

        tracker.Tell(new DownloadRequestActor.QueryHistory("tracker-7"));
        var history = await ExpectMsgAsync<DownloadRequestActor.DownloadHistoryEntry>(TimeSpan.FromSeconds(5));

        Assert.Equal("movies", history.Category);
    }
}
