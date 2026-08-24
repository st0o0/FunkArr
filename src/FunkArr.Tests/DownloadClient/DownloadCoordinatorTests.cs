using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;
using Akka.Persistence;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Pipeline;
using FunkArr.DownloadClient.Queue;
using FunkArr.DownloadClient.Tracker;
using FunkArr.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.DownloadClient;

public sealed class DownloadActorTests : Akka.Hosting.TestKit.TestKit
{
    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<IFileService>(new StubFileService());
        services.AddHttpClient();
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.AddHocon(
            """
            akka.persistence.journal.plugin = "akka.persistence.journal.inmem"
            akka.persistence.snapshot-store.plugin = "akka.persistence.snapshot-store.inmem"
            """,
            HoconAddMode.Prepend);

        builder.WithActors((system, registry) =>
        {
            var trackerProbe = CreateTestProbe();
            var queueProbe = CreateTestProbe();
            registry.Register<DownloadRequestActor>(trackerProbe.Ref);
            registry.Register<QueueActor>(queueProbe.Ref);
        });
    }

    [Fact]
    public async Task FullHappyPath_DirectDownload_NoSubtitle()
    {
        var resolver = DependencyResolver.For(Sys);
        var coordinator = Sys.ActorOf(resolver.Props<DownloadActor>(), "coord-happy");

        coordinator.Tell(new StartDownload("nzo1", "https://example.com/video.mp4", null, "TestTitle"));

        await Task.Delay(200);

        coordinator.Tell(new VideoFetched("nzo1"));
        coordinator.Tell(new VideoRemuxed("nzo1"));

        await Task.Delay(200);

        coordinator.Tell(new StartDownload("nzo1", "https://example.com/other.mp4", null, "Other"));
        await ExpectNoMsgAsync(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task FullHappyPath_WithSubtitle()
    {
        var resolver = DependencyResolver.For(Sys);
        var coordinator = Sys.ActorOf(resolver.Props<DownloadActor>(), "coord-sub");

        coordinator.Tell(new StartDownload("nzo2", "https://example.com/video.mp4", "https://example.com/sub.srt", "SubTitle"));

        await Task.Delay(200);

        coordinator.Tell(new VideoFetched("nzo2"));

        await Task.Delay(200);

        coordinator.Tell(new SubtitleAcquired("nzo2", true));

        await Task.Delay(200);

        coordinator.Tell(new SubtitleConverted("nzo2"));

        await Task.Delay(200);

        coordinator.Tell(new VideoRemuxed("nzo2"));

        await Task.Delay(200);

        coordinator.Tell(new StartDownload("nzo2", "https://example.com/other.mp4", null, "Other"));
        await ExpectNoMsgAsync(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task CancelDuringFetch_TransitionsToCompleted()
    {
        var resolver = DependencyResolver.For(Sys);
        var coordinator = Sys.ActorOf(resolver.Props<DownloadActor>(), "coord-cancel");

        coordinator.Tell(new StartDownload("nzo3", "https://example.com/video.mp4", null, "CancelTest"));

        await Task.Delay(200);

        coordinator.Tell(new CancelDownload("nzo3"));

        await Task.Delay(200);

        coordinator.Tell(new StartDownload("nzo3", "https://example.com/other.mp4", null, "Other"));
        await ExpectNoMsgAsync(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task WorkerFailed_TransitionsToCompleted()
    {
        var resolver = DependencyResolver.For(Sys);
        var coordinator = Sys.ActorOf(resolver.Props<DownloadActor>(), "coord-fail");

        coordinator.Tell(new StartDownload("nzo4", "https://example.com/video.mp4", null, "FailTest"));

        await Task.Delay(200);

        coordinator.Tell(new WorkerFailed("nzo4", FailureKind.Transient, "Connection failed"));

        await Task.Delay(200);

        coordinator.Tell(new StartDownload("nzo4", "https://example.com/other.mp4", null, "Other"));
        await ExpectNoMsgAsync(TimeSpan.FromMilliseconds(300));
    }

    private sealed class StubFileService : IFileService
    {
        public void EnsureDirectoriesExist() { }
        public string GetTempVideoPath(string nzoId) => $"/tmp/{nzoId}.mp4";
        public string GetTempSubtitlePath(string nzoId, string extension = ".sub") => $"/tmp/{nzoId}{extension}";
        public string GetNormalizedSubtitlePath(string nzoId) => $"/tmp/{nzoId}.srt";
        public string GetOutputPath(string title, string? category = null) => $"/out/{title}/{title}.mkv";
        public void EnsureOutputDirectory(string title, string? category = null) { }
        public void CleanupTemp(string nzoId) { }
        public Task SaveVideoAsync(string nzoId, Stream content) => Task.CompletedTask;
        public Task SaveSubtitleAsync(string nzoId, byte[] content, string extension) => Task.CompletedTask;
        public Task<string?> NormalizeSubtitleAsync(string nzoId) => Task.FromResult<string?>($"/tmp/{nzoId}.srt");
    }
}
