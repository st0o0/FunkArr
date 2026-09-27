using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Sabnzbd")]
public sealed class SabnzbdVersionTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Version_returns_expected_version()
    {
        var response = await _fixture.Client.GetAsync("/download/api?mode=version&apikey=test-key");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("4.3.3", json.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Config_returns_json_with_misc_and_categories()
    {
        var response = await _fixture.Client.GetAsync("/download/api?mode=get_config&apikey=test-key");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var config = json.GetProperty("config");
        Assert.True(config.TryGetProperty("misc", out _));
        Assert.True(config.TryGetProperty("categories", out _));
    }

    [Fact]
    public async Task FullStatus_returns_json_with_status_object()
    {
        var downloadProbe = _fixture.GetProbe<IDownloadManager>();

        var task = _fixture.Client.GetAsync("/download/api?mode=fullstatus&apikey=test-key");

        var msg = downloadProbe.ExpectMsg<QueryQueue>();
        downloadProbe.Reply(new QueueResult([], 0, 0, false, false, null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("status", out var status));
        Assert.True(status.TryGetProperty("paused", out _));
        Assert.True(status.TryGetProperty("completedir", out _));
    }

    [Fact]
    public async Task Missing_api_key_returns_403()
    {
        var response = await _fixture.Client.GetAsync("/download/api?mode=version");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_api_key_returns_403()
    {
        var response = await _fixture.Client.GetAsync("/download/api?mode=version&apikey=wrong-key");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_mode_returns_400_with_error()
    {
        var response = await _fixture.Client.GetAsync("/download/api?mode=invalid&apikey=test-key");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("status").GetBoolean());
        Assert.True(json.TryGetProperty("error", out _));
    }
}
