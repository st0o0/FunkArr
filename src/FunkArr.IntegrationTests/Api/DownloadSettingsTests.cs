using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class DownloadSettingsTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task GetSettings_ReturnsTypedResponse()
    {
        var response = await _fixture.Client.GetAsync("/api/downloads/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DownloadSettingsResponse>();
        Assert.NotNull(result);
        Assert.True(result.ConcurrentDownloads >= 0);
        Assert.NotNull(result.Schedule);
    }
}
