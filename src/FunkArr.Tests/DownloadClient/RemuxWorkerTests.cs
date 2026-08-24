using Akka.Actor;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Ffmpeg;
using FunkArr.DownloadClient.Pipeline;
using FunkArr.DownloadClient.Tracker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.DownloadClient;

public sealed class RemuxActorTests : Akka.Hosting.TestKit.TestKit
{
    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
    }

    protected override void ConfigureAkka(Akka.Hosting.AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task RemuxVideo_Success_SendsVideoRemuxed()
    {
        var ffmpeg = new FakeFfmpegService();
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new RemuxActor(ffmpeg)));

        worker.Tell(new RemuxVideo("nzo1", "TestTitle", true));

        var result = await parent.ExpectMsgAsync<VideoRemuxed>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo1", result.NzoId);
        Assert.True(ffmpeg.RemuxCalled);
    }

    [Fact]
    public async Task RemuxVideo_Failure_SendsWorkerFailed()
    {
        var ffmpeg = new FakeFfmpegService { ShouldFail = true };
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new RemuxActor(ffmpeg)));

        worker.Tell(new RemuxVideo("nzo2", "FailTitle", false));

        var result = await parent.ExpectMsgAsync<WorkerFailed>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo2", result.NzoId);
        Assert.Equal(FailureKind.Malformed, result.Kind);
    }

    private sealed class FakeFfmpegService : IFfmpegService
    {
        public bool RemuxCalled { get; private set; }
        public bool ShouldFail { get; set; }

        public Task DownloadHlsAsync(string nzoId, string url, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> HasSubtitleStreamAsync(string manifestUrl, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> ExtractSubtitleAsync(string nzoId, string manifestUrl, CancellationToken ct = default) => Task.FromResult(false);
        public Task<string> RemuxAsync(string nzoId, string title, bool hasSubtitle, string? category = null, CancellationToken ct = default)
        {
            RemuxCalled = true;
            return ShouldFail
                ? Task.FromException<string>(new InvalidOperationException("ffmpeg failed"))
                : Task.FromResult($"/out/{title}/{title}.mkv");
        }
    }
}
