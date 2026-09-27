using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Sabnzbd")]
public sealed class SabnzbdDownloadTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Retry_returns_success()
    {
        var downloadProbe = _fixture.GetProbe<IDownloadManager>();
        var downloadId = Guid.NewGuid();

        var task = _fixture.Client.GetAsync(
            $"/download/api?mode=retry&value={downloadId}&apikey=test-key");

        var historyProbe = _fixture.GetProbe<IDownloadHistoryManager>();
        historyProbe.ExpectMsg<RemoveHistoryEntry>();

        var msg = downloadProbe.ExpectMsg<RetryDownload>();
        Assert.Equal(downloadId, msg.DownloadId);
        downloadProbe.Reply(new RetryDownloadResult(true, null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("status").GetBoolean());
    }

    [Fact]
    public async Task Pause_returns_success()
    {
        var downloadProbe = _fixture.GetProbe<IDownloadManager>();

        var task = _fixture.Client.GetAsync("/download/api?mode=pause&apikey=test-key");

        var msg = downloadProbe.ExpectMsg<PauseDownloads>();
        downloadProbe.Reply(new PauseDownloadsResult(true));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("status").GetBoolean());
    }

    [Fact]
    public async Task Resume_returns_success()
    {
        var downloadProbe = _fixture.GetProbe<IDownloadManager>();

        var task = _fixture.Client.GetAsync("/download/api?mode=resume&apikey=test-key");

        var msg = downloadProbe.ExpectMsg<ResumeDownloads>();
        downloadProbe.Reply(new ResumeDownloadsResult(true));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("status").GetBoolean());
    }
}
