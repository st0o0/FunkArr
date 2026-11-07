using FunkArr.ArrApi.Newznab;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class UrlValidationTests
{
    [Theory]
    [InlineData("http://example.com/video.mp4")]
    [InlineData("http://cdn.ard.de/path/to/video")]
    public void HasAllowedScheme_accepts_http(string url) =>
        Assert.True(UrlValidation.HasAllowedScheme(url));

    [Theory]
    [InlineData("https://example.com/video.mp4")]
    [InlineData("https://cdn.zdf.de/path/to/video")]
    public void HasAllowedScheme_accepts_https(string url) =>
        Assert.True(UrlValidation.HasAllowedScheme(url));

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("file:///proc/self/environ")]
    [InlineData("ftp://internal/file")]
    [InlineData("gopher://example.com/resource")]
    public void HasAllowedScheme_rejects_non_http_schemes(string url) =>
        Assert.False(UrlValidation.HasAllowedScheme(url));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("just-text")]
    public void HasAllowedScheme_rejects_null_empty_and_malformed(string? url) =>
        Assert.False(UrlValidation.HasAllowedScheme(url));
}
