using Akka.Actor;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Ffmpeg;
using FunkArr.DownloadClient.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.DownloadClient;

public sealed class SubtitleExtractActorTests : Akka.Hosting.TestKit.TestKit
{
    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
    }

    protected override void ConfigureAkka(Akka.Hosting.AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task AcquireSubtitle_Found_SendsSubtitleAcquiredTrue()
    {
        var ffmpeg = new FakeFfmpegService { ExtractResult = true };
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new SubtitleExtractActor(ffmpeg)));

        worker.Tell(new AcquireSubtitle("nzo1", null, "https://example.com/stream.m3u8"));

        var result = await parent.ExpectMsgAsync<SubtitleAcquired>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo1", result.NzoId);
        Assert.True(result.Found);
    }

    [Fact]
    public async Task AcquireSubtitle_NotFound_SendsSubtitleAcquiredFalse()
    {
        var ffmpeg = new FakeFfmpegService { ExtractResult = false };
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new SubtitleExtractActor(ffmpeg)));

        worker.Tell(new AcquireSubtitle("nzo2", null, "https://example.com/stream.m3u8"));

        var result = await parent.ExpectMsgAsync<SubtitleAcquired>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo2", result.NzoId);
        Assert.False(result.Found);
    }

    private sealed class FakeFfmpegService : IFfmpegService
    {
        public bool ExtractResult { get; set; }

        public Task DownloadHlsAsync(string nzoId, string url, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> HasSubtitleStreamAsync(string manifestUrl, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> ExtractSubtitleAsync(string nzoId, string manifestUrl, CancellationToken ct = default) => Task.FromResult(ExtractResult);
        public Task<string> RemuxAsync(string nzoId, string title, bool hasSubtitle, string? category = null, CancellationToken ct = default) => Task.FromResult("output.mkv");
    }
}
