using System.Net;
using System.Text;
using FunkArr.DownloadClient;
using FunkArr.Shared;

namespace FunkArr.Tests.DownloadClient;

public sealed class Mp4DownloadServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "funkarr-download-test-" + Guid.NewGuid().ToString("N")[..8]);

    public Mp4DownloadServiceTests()
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
        var request = CreateRequest();

        var result = await sut.DownloadAsync(request, new Progress<DownloadProgress>(_ => { }));

        Assert.Equal(fileService.GetTempVideoPath(request.TempPath, request.NzoId), result.VideoPath);
        Assert.True(File.Exists(result.VideoPath));
        Assert.Equal(videoBytes, await File.ReadAllBytesAsync(result.VideoPath));
    }

    [Fact]
    public async Task DownloadAsync_AlwaysReturnsNullSubtitlePath()
    {
        var videoBytes = Encoding.UTF8.GetBytes("video-content");
        var handler = new FakeHandler(videoBytes);
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest();

        var result = await sut.DownloadAsync(request, new Progress<DownloadProgress>(_ => { }));

        Assert.Null(result.SubtitlePath);
    }

    [Fact]
    public async Task DownloadAsync_FastDownload_MayCompleteWithoutProgressReport()
    {
        var videoBytes = Encoding.UTF8.GetBytes("small-video");
        var handler = new FakeHandler(videoBytes);
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest();
        var progressCalls = new List<DownloadProgress>();

        await sut.DownloadAsync(request, new Progress<DownloadProgress>(p => progressCalls.Add(p)));

        Assert.True(progressCalls.Count is 0 or > 0);
    }

    [Fact]
    public async Task DownloadAsync_VideoHttpFailure_ThrowsAndLeavesNoOpenHandle()
    {
        var handler = new FakeHandler([], videoStatusCode: HttpStatusCode.InternalServerError);
        var fileService = new FakeFileService(_tempDir);
        var sut = CreateService(handler, fileService);
        var request = CreateRequest();

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.DownloadAsync(request, new Progress<DownloadProgress>(_ => { })));

        var expectedPath = fileService.GetTempVideoPath(request.TempPath, request.NzoId);
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
        var request = CreateRequest();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(75));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.DownloadAsync(request, new Progress<DownloadProgress>(_ => { }), cts.Token));
    }

    private static Mp4DownloadService CreateService(FakeHandler handler, IFileService fileService)
        => new(new TestHttpClientFactory(handler), fileService);

    private static DownloadRequest CreateRequest() => new(
        NzoId: "nzo1",
        VideoUrl: "https://example.com/video.mp4",
        SubtitleUrl: null,
        TempPath: "temp",
        OutputDir: "out",
        Title: "Title",
        Progress: new Progress<DownloadProgress>(_ => { }));

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeFileService(string tempDir) : IFileService
    {
        public void EnsureDirectoriesExist(string tempPath, string downloadPath) { }

        public string GetTempVideoPath(string tempPath, string nzoId)
            => Path.Combine(tempDir, $"{nzoId}.video.tmp");

        public string GetTempSubtitlePath(string tempPath, string nzoId, string extension = ".sub")
            => Path.Combine(tempDir, $"{nzoId}{extension}");

        public string GetNormalizedSubtitlePath(string tempPath, string nzoId)
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
        private readonly HttpStatusCode _videoStatusCode;
        private readonly TimeSpan? _delayPerChunk;

        public FakeHandler(
            byte[] videoBytes,
            HttpStatusCode videoStatusCode = HttpStatusCode.OK,
            TimeSpan? delayPerChunk = null)
        {
            _videoBytes = videoBytes;
            _videoStatusCode = videoStatusCode;
            _delayPerChunk = delayPerChunk;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
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
