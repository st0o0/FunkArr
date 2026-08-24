using Akka.Actor;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Ffmpeg;
using FunkArr.DownloadClient.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.DownloadClient;

public sealed class HlsDownloadActorTests : Akka.Hosting.TestKit.TestKit
{
    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
    }

    protected override void ConfigureAkka(Akka.Hosting.AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task FetchVideo_Success_SendsVideoFetched()
    {
        var ffmpeg = new FakeFfmpegService();
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new HlsDownloadActor(ffmpeg)));

        worker.Tell(new FetchVideo("nzo1", "https://example.com/stream.m3u8"));

        var result = await parent.ExpectMsgAsync<VideoFetched>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo1", result.NzoId);
        Assert.True(ffmpeg.DownloadHlsCalled);
    }

    [Fact]
    public async Task FetchVideo_Failure_SendsWorkerFailed()
    {
        var ffmpeg = new FakeFfmpegService { ShouldFail = true };
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new HlsDownloadActor(ffmpeg)));

        worker.Tell(new FetchVideo("nzo2", "https://example.com/stream.m3u8"));

        var result = await parent.ExpectMsgAsync<WorkerFailed>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo2", result.NzoId);
    }

    private sealed class FakeFfmpegService : IFfmpegService
    {
        public bool DownloadHlsCalled { get; private set; }
        public bool ShouldFail { get; set; }

        public Task DownloadHlsAsync(string nzoId, string url, CancellationToken ct = default)
        {
            DownloadHlsCalled = true;
            return ShouldFail ? Task.FromException(new InvalidOperationException("ffmpeg failed")) : Task.CompletedTask;
        }

        public Task<bool> HasSubtitleStreamAsync(string manifestUrl, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> ExtractSubtitleAsync(string nzoId, string manifestUrl, CancellationToken ct = default) => Task.FromResult(false);
        public Task<string> RemuxAsync(string nzoId, string title, bool hasSubtitle, string? category = null, CancellationToken ct = default) => Task.FromResult("output.mkv");
    }
}
