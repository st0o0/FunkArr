using System.Net;

namespace FunkArr.ArrApi.Tests.Integration;

public sealed class SabnzbdIntegrationTests : IAsyncLifetime
{
    private FunkArrTestServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await FunkArrTestServer.CreateAsync();
    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task Version_returns_ok_with_valid_api_key()
    {
        var response = await _server.Client.GetAsync("/download/api?mode=version&apikey=test-key");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("version", content);
    }

    [Fact]
    public async Task Request_without_api_key_returns_forbidden()
    {
        var response = await _server.Client.GetAsync("/download/api?mode=version");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_with_wrong_api_key_returns_forbidden()
    {
        var response = await _server.Client.GetAsync("/download/api?mode=version&apikey=wrong-key");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_mode_returns_bad_request()
    {
        var response = await _server.Client.GetAsync("/download/api?mode=invalid&apikey=test-key");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
