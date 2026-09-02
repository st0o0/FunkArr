using System.Text;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;
using Microsoft.AspNetCore.Http;

namespace FunkArr.ArrApi.Tests;

public sealed class ApiKeyEndpointFilterTests
{
    [Fact]
    public void Newznab_error_factory_returns_xml()
    {
        var errorResult = NewznabErrorFactory();

        Assert.NotNull(errorResult);
    }

    [Fact]
    public void Sabnzbd_error_factory_returns_json()
    {
        var errorResult = SabnzbdErrorFactory();

        Assert.NotNull(errorResult);
    }

    private static IResult NewznabErrorFactory() =>
        Results.Content(
            IndexerApiEndpoints.Serialize(NewznabError.InvalidApiKey),
            "application/xml",
            Encoding.UTF8,
            403);

    private static IResult SabnzbdErrorFactory() =>
        Results.Json(new { status = false, error = "API Key Incorrect" }, statusCode: 403);
}
