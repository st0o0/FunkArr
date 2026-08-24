using Akka.Actor;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Pipeline;
using FunkArr.DownloadClient.Tracker;
using FunkArr.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.DownloadClient;

public sealed class SubtitleConvertActorTests : Akka.Hosting.TestKit.TestKit
{
    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
    }

    protected override void ConfigureAkka(Akka.Hosting.AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task ConvertSubtitle_Success_SendsSubtitleConverted()
    {
        var fileService = new FakeFileService { NormalizeResult = "/tmp/nzo1.srt" };
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new SubtitleConvertActor(fileService)));

        worker.Tell(new ConvertSubtitle("nzo1"));

        var result = await parent.ExpectMsgAsync<SubtitleConverted>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo1", result.NzoId);
    }

    [Fact]
    public async Task ConvertSubtitle_NullResult_SendsWorkerFailed()
    {
        var fileService = new FakeFileService { NormalizeResult = null };
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new SubtitleConvertActor(fileService)));

        worker.Tell(new ConvertSubtitle("nzo2"));

        var result = await parent.ExpectMsgAsync<WorkerFailed>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo2", result.NzoId);
        Assert.Equal(FailureKind.Malformed, result.Kind);
    }

    private sealed class FakeFileService : IFileService
    {
        public string? NormalizeResult { get; set; }

        public void EnsureDirectoriesExist() { }
        public string GetTempVideoPath(string nzoId) => $"temp/{nzoId}.mp4";
        public string GetTempSubtitlePath(string nzoId, string extension = ".sub") => $"temp/{nzoId}{extension}";
        public string GetNormalizedSubtitlePath(string nzoId) => $"temp/{nzoId}.srt";
        public string GetOutputPath(string title, string? category = null) => $"out/{title}/{title}.mkv";
        public void EnsureOutputDirectory(string title, string? category = null) { }
        public void CleanupTemp(string nzoId) { }
        public Task SaveVideoAsync(string nzoId, Stream content) => Task.CompletedTask;
        public Task SaveSubtitleAsync(string nzoId, byte[] content, string extension) => Task.CompletedTask;
        public Task<string?> NormalizeSubtitleAsync(string nzoId) => Task.FromResult(NormalizeResult);
    }
}
