using System.Net;
using System.Text;
using Akka.Actor;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Pipeline;
using FunkArr.DownloadClient.Tracker;
using FunkArr.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunkArr.Tests.DownloadClient;

public sealed class Mp4DownloadActorTests : Akka.Hosting.TestKit.TestKit
{
    private readonly MockFileService _fileService = new();

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddHttpClient();
    }

    protected override void ConfigureAkka(Akka.Hosting.AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task FetchVideo_Success_SendsVideoFetched()
    {
        var handler = new FakeHandler(Encoding.UTF8.GetBytes("video-content"), HttpStatusCode.OK);
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new Mp4DownloadActor(new TestHttpClientFactory(handler), _fileService)));

        worker.Tell(new FetchVideo("nzo1", "https://example.com/video.mp4"));

        var result = await parent.ExpectMsgAsync<VideoFetched>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo1", result.NzoId);
        Assert.True(_fileService.SaveVideoCalled);
        Assert.Equal("nzo1", _fileService.LastSavedVideoNzoId);
    }

    [Fact]
    public async Task FetchVideo_HttpFailure_SendsWorkerFailed()
    {
        var handler = new FakeHandler([], HttpStatusCode.InternalServerError);
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new Mp4DownloadActor(new TestHttpClientFactory(handler), _fileService)));

        worker.Tell(new FetchVideo("nzo2", "https://example.com/video.mp4"));

        var result = await parent.ExpectMsgAsync<WorkerFailed>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo2", result.NzoId);
        Assert.Equal(FailureKind.Transient, result.Kind);
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeHandler(byte[] content, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content),
            };
            response.Content.Headers.ContentLength = content.Length;
            return Task.FromResult(response);
        }
    }

    private sealed class MockFileService : IFileService
    {
        public bool SaveVideoCalled { get; private set; }
        public string? LastSavedVideoNzoId { get; private set; }

        public void EnsureDirectoriesExist() { }
        public string GetTempVideoPath(string nzoId) => $"temp/{nzoId}.mp4";
        public string GetTempSubtitlePath(string nzoId, string extension = ".sub") => $"temp/{nzoId}{extension}";
        public string GetNormalizedSubtitlePath(string nzoId) => $"temp/{nzoId}.srt";
        public string GetOutputPath(string title, string? category = null) => $"out/{title}/{title}.mkv";
        public void EnsureOutputDirectory(string title, string? category = null) { }
        public void CleanupTemp(string nzoId) { }
        public Task SaveVideoAsync(string nzoId, Stream content)
        {
            SaveVideoCalled = true;
            LastSavedVideoNzoId = nzoId;
            return Task.CompletedTask;
        }
        public Task SaveSubtitleAsync(string nzoId, byte[] content, string extension) => Task.CompletedTask;
        public Task<string?> NormalizeSubtitleAsync(string nzoId) => Task.FromResult<string?>($"temp/{nzoId}.srt");
    }
}

public sealed class SubtitleDownloadActorTests : Akka.Hosting.TestKit.TestKit
{
    private readonly MockFileService _fileService = new();

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddHttpClient();
    }

    protected override void ConfigureAkka(Akka.Hosting.AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task AcquireSubtitle_Success_SendsSubtitleAcquired()
    {
        var handler = new FakeHandler(Encoding.UTF8.GetBytes("1\n00:00:00,000 --> 00:00:01,000\nHi\n"), HttpStatusCode.OK);
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new SubtitleDownloadActor(new TestHttpClientFactory(handler), _fileService)));

        worker.Tell(new AcquireSubtitle("nzo3", "https://example.com/sub.srt", null));

        var result = await parent.ExpectMsgAsync<SubtitleAcquired>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo3", result.NzoId);
        Assert.True(result.Found);
        Assert.True(_fileService.SaveSubtitleCalled);
    }

    [Fact]
    public async Task AcquireSubtitle_NotFound_SendsSubtitleNotFound()
    {
        var handler = new FakeHandler([], HttpStatusCode.NotFound);
        var parent = CreateTestProbe();
        var worker = parent.ChildActorOf(Props.Create(() => new SubtitleDownloadActor(new TestHttpClientFactory(handler), _fileService)));

        worker.Tell(new AcquireSubtitle("nzo4", "https://example.com/sub.srt", null));

        var result = await parent.ExpectMsgAsync<SubtitleAcquired>(TimeSpan.FromSeconds(5));
        Assert.Equal("nzo4", result.NzoId);
        Assert.False(result.Found);
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeHandler(byte[] content, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content),
            };
            response.Content.Headers.ContentLength = content.Length;
            return Task.FromResult(response);
        }
    }

    private sealed class MockFileService : IFileService
    {
        public bool SaveSubtitleCalled { get; private set; }

        public void EnsureDirectoriesExist() { }
        public string GetTempVideoPath(string nzoId) => $"temp/{nzoId}.mp4";
        public string GetTempSubtitlePath(string nzoId, string extension = ".sub") => $"temp/{nzoId}{extension}";
        public string GetNormalizedSubtitlePath(string nzoId) => $"temp/{nzoId}.srt";
        public string GetOutputPath(string title, string? category = null) => $"out/{title}/{title}.mkv";
        public void EnsureOutputDirectory(string title, string? category = null) { }
        public void CleanupTemp(string nzoId) { }
        public Task SaveVideoAsync(string nzoId, Stream content) => Task.CompletedTask;
        public Task SaveSubtitleAsync(string nzoId, byte[] content, string extension)
        {
            SaveSubtitleCalled = true;
            return Task.CompletedTask;
        }
        public Task<string?> NormalizeSubtitleAsync(string nzoId) => Task.FromResult<string?>($"temp/{nzoId}.srt");
    }
}
