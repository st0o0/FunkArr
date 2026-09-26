using System.Net;

namespace FunkArr.ArrApi.Tests.Integration;

public sealed class NewznabIntegrationTests : IAsyncLifetime
{
    private FunkArrTestServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await FunkArrTestServer.CreateAsync();
    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task Caps_returns_xml_with_valid_api_key()
    {
        var response = await _server.Client.GetAsync("/index/api?t=caps&apikey=test-key");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<caps", content);
    }

    [Fact]
    public async Task Request_without_api_key_returns_error_xml()
    {
        var response = await _server.Client.GetAsync("/index/api?t=caps");

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("error", content);
        Assert.Contains("100", content);
    }

    [Fact]
    public async Task Request_with_wrong_api_key_returns_error_xml()
    {
        var response = await _server.Client.GetAsync("/index/api?t=caps&apikey=wrong-key");

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("error", content);
        Assert.Contains("100", content);
    }
}
