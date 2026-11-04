using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models.Shared;
using FunkArr.Core.Actors;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class DownloadPipelineTests(FunkArrFixture fixture)
{
    [Fact]
    public async Task Pause_ReturnsOperationResult()
    {
        var task = fixture.Client.PostAsync("/api/downloads/queue/pause", null, TestContext.Current.CancellationToken);

        var probe = fixture.GetProbe<IDownloadManager>();
        probe.ExpectMsg<PauseDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new PauseDownloadsResult(true));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Resume_ReturnsOperationResult()
    {
        var task = fixture.Client.PostAsync("/api/downloads/queue/resume", null, TestContext.Current.CancellationToken);

        var probe = fixture.GetProbe<IDownloadManager>();
        probe.ExpectMsg<ResumeDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new ResumeDownloadsResult(true));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }
}
