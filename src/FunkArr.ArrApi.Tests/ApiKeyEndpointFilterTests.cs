using System.Text;
using System.Text.Json;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.ArrApi.Tests;

public sealed class ApiKeyEndpointFilterTests
{
    [Fact]
    public async Task Newznab_error_factory_returns_xml()
    {
        var httpContext = CreateHttpContext();

        await NewznabErrorFactory().ExecuteAsync(httpContext);

        Assert.Equal(403, httpContext.Response.StatusCode);
        Assert.StartsWith("application/xml", httpContext.Response.ContentType);
        var body = ReadBody(httpContext);
        Assert.Contains("Invalid API Key", body);
    }

    [Fact]
    public async Task Sabnzbd_error_factory_returns_json()
    {
        var httpContext = CreateHttpContext();

        await SabnzbdErrorFactory().ExecuteAsync(httpContext);

        Assert.Equal(403, httpContext.Response.StatusCode);
        var body = ReadBody(httpContext);
        var json = JsonDocument.Parse(body);
        Assert.False(json.RootElement.GetProperty("status").GetBoolean());
        Assert.Equal("API Key Incorrect", json.RootElement.GetProperty("error").GetString());
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static string ReadBody(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static IResult NewznabErrorFactory() =>
        Results.Content(
            NewznabXmlResult.Serialize(NewznabError.InvalidApiKey),
            "application/xml",
            Encoding.UTF8,
            403);

    private static IResult SabnzbdErrorFactory() =>
        Results.Json(new { status = false, error = "API Key Incorrect" }, statusCode: 403);
}
