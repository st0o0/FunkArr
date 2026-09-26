using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;

namespace FunkArr.Api.Tests.Integration;

public sealed class SystemApiIntegrationTests : IAsyncLifetime
{
    private FunkArrTestServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await FunkArrTestServer.CreateAsync();
    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task GetVersion_returns_ok_with_version()
    {
        var response = await _server.Client.GetAsync("/api/system/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<VersionResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.AppVersion);
    }

    [Fact]
    public async Task GetStorage_returns_ok()
    {
        var response = await _server.Client.GetAsync("/api/system/storage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<StorageStatusResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.CompleteDirectory);
        Assert.NotNull(result.IncompleteDirectory);
    }

    [Fact]
    public async Task Alive_returns_ok()
    {
        var response = await _server.Client.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
