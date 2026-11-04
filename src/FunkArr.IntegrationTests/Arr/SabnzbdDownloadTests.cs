using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunkArr.Core.Actors;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Sabnzbd")]
public sealed class SabnzbdDownloadTests(FunkArrFixture fixture)
{
    [Fact]
    public async Task Retry_returns_success()
    {
        var downloadProbe = fixture.GetProbe<IDownloadManager>();
        var downloadId = Guid.NewGuid();

        var task = fixture.Client.GetAsync($"/download/api?mode=retry&value={downloadId}&apikey=test-key", TestContext.Current.CancellationToken);

        var historyProbe = fixture.GetProbe<IDownloadHistoryManager>();
        historyProbe.ExpectMsg<RemoveHistoryEntry>(cancellationToken: TestContext.Current.CancellationToken);

        var msg = downloadProbe.ExpectMsg<RetryDownload>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(downloadId, msg.DownloadId);
        downloadProbe.Reply(new RetryDownloadResult(true, null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.GetProperty("status").GetBoolean());
    }

    [Fact]
    public async Task Pause_returns_success()
    {
        var downloadProbe = fixture.GetProbe<IDownloadManager>();

        var task = fixture.Client.GetAsync("/download/api?mode=pause&apikey=test-key", TestContext.Current.CancellationToken);

        downloadProbe.ExpectMsg<PauseDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        downloadProbe.Reply(new PauseDownloadsResult(true));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.GetProperty("status").GetBoolean());
    }

    [Fact]
    public async Task Resume_returns_success()
    {
        var downloadProbe = fixture.GetProbe<IDownloadManager>();

        var task = fixture.Client.GetAsync("/download/api?mode=resume&apikey=test-key", TestContext.Current.CancellationToken);

        downloadProbe.ExpectMsg<ResumeDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        downloadProbe.Reply(new ResumeDownloadsResult(true));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.GetProperty("status").GetBoolean());
    }
}
