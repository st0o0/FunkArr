using System.Net;
using FunkArr.Configuration;
using FunkArr.Search;
using FunkArr.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.Search;

public class QualityProbeServiceTests
{
    private static QualityOptions DefaultOptions() => new()
    {
        Probing = true,
        CacheTtlMinutes = 360,
        CacheCapacity = 50000,
    };

    private static QualityProbeService CreateService(
        HttpMessageHandler? handler = null,
        QualityOptions? options = null)
    {
        var opts = options ?? DefaultOptions();
        var factory = new TestHttpClientFactory(handler ?? new FakeHandler(HttpStatusCode.OK));
        return new QualityProbeService(factory, NullLogger<QualityProbeService>.Instance, Options.Create(opts));
    }

    [Fact]
    public async Task ProbeAsync_WhenDisabled_ReturnsEstimated()
    {
        var opts = DefaultOptions();
        opts.Probing = false;
        var service = CreateService(options: opts);

        var result = await service.ProbeAsync("https://example.com/video.mp4", QualityTier.HD720, 3600);

        Assert.Equal(ProbeSource.Estimated, result.ProbeSource);
        Assert.Equal(QualityTier.HD720, result.QualityTier);
    }

    [Fact]
    public async Task ProbeAsync_HeadSuccess_ReturnsSizeFromContentLength()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, contentLength: 1_500_000_000, contentType: "video/mp4");
        var service = CreateService(handler);

        var result = await service.ProbeAsync("https://example.com/video.mp4", QualityTier.HD720, 3600);

        Assert.Equal(1_500_000_000, result.FileSize);
    }

    [Fact]
    public async Task ProbeAsync_HeadFails_ReturnsEstimatedSize()
    {
        var handler = new FakeHandler(HttpStatusCode.Forbidden);
        var service = CreateService(handler);

        var result = await service.ProbeAsync("https://example.com/video.mp4", QualityTier.HD720, 3600);

        Assert.Equal(QualityProbeService.EstimateSize(3600, QualityTier.HD720), result.FileSize);
    }

    [Fact]
    public async Task ProbeAsync_ZdfUrl_UsesUrlPattern()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, contentLength: 2_000_000_000, contentType: "video/mp4");
        var service = CreateService(handler);

        var result = await service.ProbeAsync(
            "https://nrodlzdf-a.akamaihd.net/none/zdf/24/05/abc/2/6660k_p37v17.mp4",
            QualityTier.HD720, 3600);

        Assert.Equal(ProbeSource.UrlPattern, result.ProbeSource);
        Assert.Equal(1080, result.Resolution.Height);
        Assert.Equal("h265", result.Codec);
        Assert.Equal(3600L * 6660 * 1000 / 8, result.FileSize);
    }

    [Fact]
    public async Task ProbeAsync_CachesResult()
    {
        var callCount = 0;
        var handler = new FakeHandler(HttpStatusCode.OK, contentLength: 1000, contentType: "video/mp4",
            onSend: () => Interlocked.Increment(ref callCount));
        var service = CreateService(handler);

        var url = "https://example.com/cache-test.mp4";
        await service.ProbeAsync(url, QualityTier.HD720, 100);
        var countAfterFirst = callCount;
        await service.ProbeAsync(url, QualityTier.HD720, 100);

        Assert.Equal(countAfterFirst, callCount);
    }

    [Fact]
    public async Task ProbeAsync_HlsUrl_SkipsContainerProbe()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, contentLength: 0, contentType: "application/x-mpegURL");
        var service = CreateService(handler);

        var result = await service.ProbeAsync(
            "https://example.com/master.m3u8", QualityTier.HD720, 3600);

        Assert.NotEqual(ProbeSource.ContainerHeader, result.ProbeSource);
    }

    [Fact]
    public async Task ExpandWithProbingAsync_CreatesVariants()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, contentLength: 500_000_000, contentType: "video/mp4");
        var service = CreateService(handler);

        var item = new MediathekResultItem
        {
            Title = "Test",
            Topic = "Test Topic",
            Channel = "ZDF",
            Url_Video_HD = "https://example.com/hd.mp4",
            Url_Video = "https://example.com/sd.mp4",
            Url_Video_Low = "",
            Duration = 3600,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        var results = await service.ExpandWithProbingAsync(item, 30, 0);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.NotNull(r.QualityInfo));
    }

    [Fact]
    public void EstimateSize_CalculatesCorrectly()
    {
        Assert.Equal(1_800_000_000, QualityProbeService.EstimateSize(3600, QualityTier.HD1080));
        Assert.Equal(900_000_000, QualityProbeService.EstimateSize(3600, QualityTier.HD720));
        Assert.Equal(360_000_000, QualityProbeService.EstimateSize(3600, QualityTier.SD));
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly long? _contentLength;
        private readonly string? _contentType;
        private readonly Action? _onSend;

        public FakeHandler(HttpStatusCode statusCode, long? contentLength = null,
            string? contentType = null, Action? onSend = null)
        {
            _statusCode = statusCode;
            _contentLength = contentLength;
            _contentType = contentType;
            _onSend = onSend;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _onSend?.Invoke();
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new ByteArrayContent([]),
            };
            if (_contentLength is not null)
            {
                response.Content.Headers.ContentLength = _contentLength;
            }

            if (_contentType is not null)
            {
                response.Content.Headers.TryAddWithoutValidation("Content-Type", _contentType);
            }

            return Task.FromResult(response);
        }
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
