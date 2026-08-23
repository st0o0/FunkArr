using FunkArr.Search;

namespace FunkArr.Tests.Search;

public class UrlPatternAnalyzerTests
{
    [Theory]
    [InlineData("https://nrodlzdf-a.akamaihd.net/none/zdf/24/05/240521_sendung_hjo/2/2256k_p18v17.mp4", 720, "h264", 2256)]
    [InlineData("https://nrodlzdf-a.akamaihd.net/none/zdf/24/05/240521_sendung_hjo/2/6660k_p37v17.mp4", 1080, "h265", 6660)]
    [InlineData("https://nrodlzdf-a.akamaihd.net/none/3sat/24/01/240115_sendung/2/808k_p11v17.mp4", 360, "h264", 808)]
    [InlineData("https://nrodlzdf-a.akamaihd.net/none/zdf/24/01/240115_sendung/2/3328k_p35v17.mp4", 720, "h265", 3328)]
    [InlineData("https://nrodlzdf-a.akamaihd.net/none/zdf/24/01/240115_sendung/2/3360k_p15v17.mp4", 720, "h264", 3360)]
    public void Analyze_ZdfUrls_ExtractsBitrateAndProfile(
        string url, int expectedHeight, string expectedCodec, int expectedBitrate)
    {
        var result = UrlPatternAnalyzer.Analyze(url);

        Assert.NotNull(result);
        Assert.Equal(expectedHeight, result.Resolution!.Value.Height);
        Assert.Equal(expectedCodec, result.Codec);
        Assert.Equal(expectedBitrate, result.BitrateKbps);
    }

    [Theory]
    [InlineData("https://pdvideosdaserste-a.akamaihd.net/int/2024/05/21/abc123/720/master.m3u8", 720)]
    [InlineData("https://pdvideosdaserste-a.akamaihd.net/int/2024/05/21/abc123/1080/master.m3u8", 1080)]
    [InlineData("https://pdvideosdaserste-a.akamaihd.net/int/2024/05/21/abc123/480/file.mp4", 480)]
    public void Analyze_ArdUrls_ExtractsResolution(string url, int expectedHeight)
    {
        var result = UrlPatternAnalyzer.Analyze(url);

        Assert.NotNull(result);
        Assert.Equal(expectedHeight, result.Resolution!.Value.Height);
    }

    [Theory]
    [InlineData("https://arteptweb-a.akamaihd.net/am/ptweb/089000/089400/089487_1080p.mp4", 1080)]
    [InlineData("https://arteptweb-a.akamaihd.net/am/ptweb/089000/089400/089487_720p.mp4", 720)]
    public void Analyze_ArteUrls_ExtractsResolution(string url, int expectedHeight)
    {
        var result = UrlPatternAnalyzer.Analyze(url);

        Assert.NotNull(result);
        Assert.Equal(expectedHeight, result.Resolution!.Value.Height);
    }

    [Theory]
    [InlineData("https://example.com/video.mp4")]
    [InlineData("https://random-cdn.com/content/stream")]
    [InlineData("")]
    public void Analyze_UnknownUrls_ReturnsNull(string url)
    {
        var result = UrlPatternAnalyzer.Analyze(url);
        Assert.Null(result);
    }

    [Fact]
    public void Analyze_NullUrl_ReturnsNull()
    {
        Assert.Null(UrlPatternAnalyzer.Analyze(null!));
    }

    [Theory]
    [InlineData("https://example.com/video/master.m3u8", true)]
    [InlineData("https://example.com/video.m3u8?token=abc", true)]
    [InlineData("https://example.com/video.mp4", false)]
    public void IsHls_DetectsM3u8(string url, bool expected)
    {
        Assert.Equal(expected, UrlPatternAnalyzer.IsHls(url));
    }

    [Theory]
    [InlineData("https://example.com/manifest.mpd", true)]
    [InlineData("https://example.com/video.mp4", false)]
    public void IsDash_DetectsMpd(string url, bool expected)
    {
        Assert.Equal(expected, UrlPatternAnalyzer.IsDash(url));
    }
}
