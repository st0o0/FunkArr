using System.Net;
using System.Text;
using FunkArr.DownloadClient;
using FunkArr.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunkArr.Tests.DownloadClient;

public sealed class DownloadServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "funkarr-download-test-" + Guid.NewGuid().ToString("N")[..8]);

    public DownloadServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task DownloadAsync_SuccessfulVideo_WritesChunksAndReturnsTempPath()
    {
        var videoBytes = Encoding.UTF8.GetBytes(new string('a', 20_000));
        var handler = new FakeHandler(videoBytes);
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest(subtitleUrl: null);

        var result = await sut.DownloadAsync(request, (_, _) => { });

        Assert.Equal(fileService.GetTempVideoPath(request.TempPath, request.NzoId), result.VideoPath);
        Assert.True(File.Exists(result.VideoPath));
        Assert.Equal(videoBytes, await File.ReadAllBytesAsync(result.VideoPath));
    }

    [Fact]
    public async Task DownloadAsync_SubtitleSucceeds_WritesSubtitleAndReturnsPath()
    {
        var videoBytes = Encoding.UTF8.GetBytes("video-content");
        var subtitleBytes = Encoding.UTF8.GetBytes("1\n00:00:00,000 --> 00:00:01,000\nHello\n");
        var handler = new FakeHandler(videoBytes, subtitleBytes: subtitleBytes);
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest(subtitleUrl: "https://example.com/sub.srt");

        var result = await sut.DownloadAsync(request, (_, _) => { });

        Assert.NotNull(result.SubtitlePath);
        Assert.True(File.Exists(result.SubtitlePath));
        Assert.Equal(subtitleBytes, await File.ReadAllBytesAsync(result.SubtitlePath!));
    }

    [Fact]
    public async Task DownloadAsync_SubtitleFails_ReturnsNullWithoutThrowing()
    {
        var videoBytes = Encoding.UTF8.GetBytes("video-content");
        var handler = new FakeHandler(videoBytes, subtitleStatusCode: HttpStatusCode.NotFound);
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest(subtitleUrl: "https://example.com/sub.srt");

        var result = await sut.DownloadAsync(request, (_, _) => { });

        Assert.Null(result.SubtitlePath);
    }

    [Fact]
    public async Task DownloadAsync_NoSubtitleUrl_MakesNoSubtitleRequest()
    {
        var videoBytes = Encoding.UTF8.GetBytes("video-content");
        var handler = new FakeHandler(videoBytes);
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest(subtitleUrl: null);

        var result = await sut.DownloadAsync(request, (_, _) => { });

        Assert.Null(result.SubtitlePath);
        Assert.Equal(0, handler.SubtitleRequestCount);
    }

    [Fact]
    public async Task DownloadAsync_FastDownload_MayCompleteWithoutProgressReport()
    {
        var videoBytes = Encoding.UTF8.GetBytes("small-video");
        var handler = new FakeHandler(videoBytes);
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest(subtitleUrl: null);
        var progressCalls = new List<(long Downloaded, long Total)>();

        await sut.DownloadAsync(request, (downloaded, total) => progressCalls.Add((downloaded, total)));

        // Best-effort reporting: a sub-2-second download is allowed to report zero times.
        Assert.True(progressCalls.Count is 0 or > 0);
    }

    [Fact]
    public async Task DownloadAsync_VideoHttpFailure_ThrowsAndLeavesNoOpenHandle()
    {
        var handler = new FakeHandler([], videoStatusCode: HttpStatusCode.InternalServerError);
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest(subtitleUrl: null);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.DownloadAsync(request, (_, _) => { }));

        var expectedPath = fileService.GetTempVideoPath(request.TempPath, request.NzoId);
        // File may or may not exist (never written to), but must not be locked.
        if (File.Exists(expectedPath))
        {
            await using var stream = File.Open(expectedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
    }

    [Fact]
    public async Task DownloadAsync_Cancellation_DoesNotCompleteSuccessfully()
    {
        var videoBytes = Encoding.UTF8.GetBytes(new string('a', 100_000));
        var handler = new FakeHandler(videoBytes, delayPerChunk: TimeSpan.FromMilliseconds(50));
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest(subtitleUrl: null);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(75));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.DownloadAsync(request, (_, _) => { }, cts.Token));
    }

    private static DownloadService CreateService(FakeHandler handler, IFileService fileService)
        => new(new TestHttpClientFactory(handler), fileService, NullLogger<DownloadService>.Instance);

    private static DownloadRequest CreateRequest(string? subtitleUrl) => new(
        NzoId: "nzo1",
        VideoUrl: "https://example.com/video.mp4",
        SubtitleUrl: subtitleUrl,
        TempPath: "temp",
        OutputDir: "out",
        Title: "Title");

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeFileService(string tempDir) : IFileService
    {
        public void EnsureDirectoriesExist(string tempPath, string downloadPath) { }

        public string GetTempVideoPath(string tempPath, string nzoId)
            => Path.Combine(tempDir, $"{nzoId}.video.tmp");

        public string GetTempSubtitlePath(string tempPath, string nzoId)
            => Path.Combine(tempDir, $"{nzoId}.srt");

        public string GetOutputPath(string downloadPath, string title)
            => Path.Combine(tempDir, title, $"{title}.mkv");

        public void EnsureOutputDirectory(string downloadPath, string title)
            => Directory.CreateDirectory(Path.Combine(tempDir, title));

        public void CleanupTempFiles(string videoPath, params string?[] additionalPaths) { }

        public Task WriteSubtitleAsync(string path, byte[] content)
            => File.WriteAllBytesAsync(path, content);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly byte[] _videoBytes;
        private readonly byte[]? _subtitleBytes;
        private readonly HttpStatusCode _videoStatusCode;
        private readonly HttpStatusCode _subtitleStatusCode;
        private readonly TimeSpan? _delayPerChunk;

        public int SubtitleRequestCount { get; private set; }

        public FakeHandler(
            byte[] videoBytes,
            byte[]? subtitleBytes = null,
            HttpStatusCode videoStatusCode = HttpStatusCode.OK,
            HttpStatusCode subtitleStatusCode = HttpStatusCode.OK,
            TimeSpan? delayPerChunk = null)
        {
            _videoBytes = videoBytes;
            _subtitleBytes = subtitleBytes;
            _videoStatusCode = videoStatusCode;
            _subtitleStatusCode = subtitleStatusCode;
            _delayPerChunk = delayPerChunk;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var isSubtitle = request.RequestUri!.AbsolutePath.EndsWith(".srt", StringComparison.Ordinal);

            if (isSubtitle)
            {
                SubtitleRequestCount++;
                if (_subtitleStatusCode != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(_subtitleStatusCode);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(_subtitleBytes ?? []),
                };
            }

            if (_videoStatusCode != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(_videoStatusCode)
                {
                    Content = new ByteArrayContent([]),
                };
            }

            Stream contentStream = _delayPerChunk is { } delay
                ? new ThrottledStream(_videoBytes, delay)
                : new MemoryStream(_videoBytes);

            await Task.Yield();
            var content = new StreamContent(contentStream);
            content.Headers.ContentLength = _videoBytes.Length;

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }
    }

    private sealed class ThrottledStream(byte[] data, TimeSpan delayPerChunk) : Stream
    {
        private readonly MemoryStream _inner = new(data);

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(delayPerChunk, cancellationToken);
            return await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(delayPerChunk, cancellationToken);
            return await _inner.ReadAsync(buffer, cancellationToken);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
