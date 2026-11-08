using FunkArr.Api.Models.Setup;
using FunkArr.Core.Configuration;
using Microsoft.AspNetCore.Http;

namespace FunkArr.Api.Tests;

public sealed class SetupSelfUrlTests
{
    private static DefaultHttpContext CreateHttpContext(string scheme = "http", string host = "localhost", int? port = 6969)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = scheme;
        ctx.Request.Host = port.HasValue ? new HostString(host, port.Value) : new HostString(host);
        return ctx;
    }

    [Fact]
    public void ResolveSelfUrl_PerRequestOverride_TakesPriority()
    {
        var request = new CreateArrResourceRequest("http://arr:8989", "key", FunkArrUrl: "http://custom:1234");
        var options = new FunkArrOptions { BaseUrl = "http://funkarr:6969" };
        var ctx = CreateHttpContext();

        var (uri, error) = SetupApiEndpoints.ResolveSelfUrl(request, options, ctx);

        Assert.Null(error);
        Assert.NotNull(uri);
        Assert.Equal("http://custom:1234/", uri.ToString());
    }

    [Fact]
    public void ResolveSelfUrl_ConfigBaseUrl_UsedWhenNoPerRequestOverride()
    {
        var request = new CreateArrResourceRequest("http://arr:8989", "key");
        var options = new FunkArrOptions { BaseUrl = "http://funkarr:6969" };
        var ctx = CreateHttpContext();

        var (uri, error) = SetupApiEndpoints.ResolveSelfUrl(request, options, ctx);

        Assert.Null(error);
        Assert.NotNull(uri);
        Assert.Equal("http://funkarr:6969/", uri.ToString());
    }

    [Fact]
    public void ResolveSelfUrl_RequestContextFallback_WhenBothEmpty()
    {
        var request = new CreateArrResourceRequest("http://arr:8989", "key");
        var options = new FunkArrOptions();
        var ctx = CreateHttpContext("https", "myhost.local", 443);

        var (uri, error) = SetupApiEndpoints.ResolveSelfUrl(request, options, ctx);

        Assert.Null(error);
        Assert.NotNull(uri);
        Assert.Equal("https://myhost.local/", uri.ToString());
    }

    [Fact]
    public void ResolveSelfUrl_MalformedUrl_ReturnsError()
    {
        var request = new CreateArrResourceRequest("http://arr:8989", "key", FunkArrUrl: "not-a-url");
        var options = new FunkArrOptions();
        var ctx = CreateHttpContext();

        var (uri, error) = SetupApiEndpoints.ResolveSelfUrl(request, options, ctx);

        Assert.Null(uri);
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Contains("Invalid FunkArr URL", error.Error);
    }

    [Fact]
    public void ResolveSelfUrl_NonHttpScheme_ReturnsError()
    {
        var request = new CreateArrResourceRequest("http://arr:8989", "key", FunkArrUrl: "ftp://funkarr:6969");
        var options = new FunkArrOptions();
        var ctx = CreateHttpContext();

        var (uri, error) = SetupApiEndpoints.ResolveSelfUrl(request, options, ctx);

        Assert.Null(uri);
        Assert.NotNull(error);
        Assert.False(error.Success);
    }

    [Fact]
    public void ResolveSelfUrl_HttpsUrl_PassesValidation()
    {
        var request = new CreateArrResourceRequest("http://arr:8989", "key", FunkArrUrl: "https://funkarr.example.com");
        var options = new FunkArrOptions();
        var ctx = CreateHttpContext();

        var (uri, error) = SetupApiEndpoints.ResolveSelfUrl(request, options, ctx);

        Assert.Null(error);
        Assert.NotNull(uri);
        Assert.Equal("https", uri.Scheme);
    }

    [Fact]
    public void ResolveSelfUrl_TrailingSlash_IsTrimmed()
    {
        var request = new CreateArrResourceRequest("http://arr:8989", "key", FunkArrUrl: "http://funkarr:6969/");
        var options = new FunkArrOptions();
        var ctx = CreateHttpContext();

        var (uri, error) = SetupApiEndpoints.ResolveSelfUrl(request, options, ctx);

        Assert.Null(error);
        Assert.NotNull(uri);
        Assert.Equal("http://funkarr:6969/", uri.ToString());
    }
}
