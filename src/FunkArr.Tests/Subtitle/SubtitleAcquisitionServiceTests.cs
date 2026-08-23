using System.Net;
using FunkArr.Shared;
using FunkArr.Subtitle;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunkArr.Tests.Subtitle;

public sealed class SubtitleAcquisitionServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "funkarr-sub-test-" + Guid.NewGuid().ToString("N")[..8]);

    public SubtitleAcquisitionServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AcquireAsync_WithSubtitleUrl_DownloadsAndReturnsPath()
    {
        var handler = new FakeSubHandler(HttpStatusCode.OK, "1\n00:00:00,000 --> 00:00:01,000\nHello\n");
        var sut = CreateService(handler);

        var result = await sut.AcquireAsync(
            subtitleUrl: "https://example.com/sub.srt",
            hlsManifestUrl: null,
            tempPath: _tempDir,
            nzoId: "test1");

        Assert.NotNull(result);
        Assert.True(File.Exists(result));
    }

    [Fact]
    public async Task AcquireAsync_SubtitleDownloadFails_ReturnsNull()
    {
        var handler = new FakeSubHandler(HttpStatusCode.NotFound, "");
        var sut = CreateService(handler);

        var result = await sut.AcquireAsync(
            subtitleUrl: "https://example.com/sub.srt",
            hlsManifestUrl: null,
            tempPath: _tempDir,
            nzoId: "test2");

        Assert.Null(result);
    }

    [Fact]
    public async Task AcquireAsync_NoSubtitleNoHls_ReturnsNull()
    {
        var handler = new FakeSubHandler(HttpStatusCode.OK, "");
        var sut = CreateService(handler);

        var result = await sut.AcquireAsync(
            subtitleUrl: null,
            hlsManifestUrl: null,
            tempPath: _tempDir,
            nzoId: "test3");

        Assert.Null(result);
    }

    [Fact]
    public async Task AcquireAsync_SubtitleUrlException_ReturnsNull()
    {
        var handler = new ThrowingHandler();
        var sut = CreateService(handler);

        var result = await sut.AcquireAsync(
            subtitleUrl: "https://example.com/sub.srt",
            hlsManifestUrl: null,
            tempPath: _tempDir,
            nzoId: "test4");

        Assert.Null(result);
    }

    private SubtitleAcquisitionService CreateService(HttpMessageHandler handler) =>
        new(new TestFactory(handler), new FileService(),
            NullLogger<SubtitleAcquisitionService>.Instance);

    private sealed class TestFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeSubHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (statusCode != HttpStatusCode.OK)
            {
                return Task.FromResult(new HttpResponseMessage(statusCode));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content),
            });
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Connection refused");
        }
    }
}
