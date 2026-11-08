using FunkArr.Api.Models.Setup;

namespace FunkArr.Api.Tests;

public sealed class ArrPayloadTests
{
    [Fact]
    public void SabnzbdPayload_HttpsUri_SetsUseSslTrue()
    {
        var uri = new Uri("https://funkarr.example.com:6969");

        var payload = SabnzbdDownloadClientPayload.Create(uri, "api-key", "tv");

        var useSsl = Assert.Single(payload.Fields, f => f.Name == "useSsl");
        Assert.Equal(true, useSsl.Value);
    }

    [Fact]
    public void SabnzbdPayload_HttpUri_SetsUseSslFalse()
    {
        var uri = new Uri("http://funkarr:6969");

        var payload = SabnzbdDownloadClientPayload.Create(uri, "api-key", "tv");

        var useSsl = Assert.Single(payload.Fields, f => f.Name == "useSsl");
        Assert.Equal(false, useSsl.Value);
    }

    [Fact]
    public void SabnzbdPayload_ExtractsHostAndPort()
    {
        var uri = new Uri("http://funkarr:6969");

        var payload = SabnzbdDownloadClientPayload.Create(uri, "api-key", "movies");

        var host = Assert.Single(payload.Fields, f => f.Name == "host");
        Assert.Equal("funkarr", host.Value);
        var port = Assert.Single(payload.Fields, f => f.Name == "port");
        Assert.Equal(6969, port.Value);
    }

    [Fact]
    public void ProwlarrPayload_UsesUriBaseUrl()
    {
        var uri = new Uri("http://funkarr:6969");

        var payload = ProwlarrIndexerPayload.Create(uri, "api-key");

        var baseUrl = Assert.Single(payload.Fields, f => f.Name == "baseUrl");
        Assert.Equal("http://funkarr:6969/index", baseUrl.Value);
    }

    [Fact]
    public void SonarrRadarrPayload_UsesUriBaseUrl()
    {
        var uri = new Uri("http://funkarr:6969");

        var payload = SonarrRadarrIndexerPayload.Create(uri, "api-key", [5030, 5040]);

        var baseUrl = Assert.Single(payload.Fields, f => f.Name == "baseUrl");
        Assert.Equal("http://funkarr:6969/index", baseUrl.Value);
    }
}
