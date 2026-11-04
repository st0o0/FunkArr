using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models.Downloads;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class DownloadSettingsTests(FunkArrFixture fixture)
{
    [Fact]
    public async Task GetSettings_ReturnsTypedResponse()
    {
        var response = await fixture.Client.GetAsync("/api/downloads/settings", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DownloadSettingsResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.ConcurrentDownloads >= 0);
        Assert.NotNull(result.Schedule);
    }
}
