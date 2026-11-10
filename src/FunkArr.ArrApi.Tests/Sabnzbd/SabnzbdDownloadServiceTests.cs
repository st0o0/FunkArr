using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.ArrApi.Sabnzbd.Services;
using FunkArr.Core.Actors;
using FunkArr.Messages.Download;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunkArr.ArrApi.Tests.Sabnzbd;

public sealed class SabnzbdDownloadServiceTests : IAsyncDisposable
{
    private readonly ActorSystem _system = ActorSystem.Create("test");
    private readonly XunitAssertions _assertions = new();

    private (SabnzbdDownloadService Service, TestProbe ManagerProbe, TestProbe HistoryProbe) CreateService()
    {
        var registry = ActorRegistry.For(_system);
        var managerProbe = new TestProbe(_system, _assertions);
        var historyProbe = new TestProbe(_system, _assertions);
        registry.Register<IDownloadManager>(managerProbe);
        registry.Register<IDownloadHistoryManager>(historyProbe);

        var service = new SabnzbdDownloadService(
            registry,
            NullLogger<SabnzbdDownloadService>.Instance);

        return (service, managerProbe, historyProbe);
    }

    [Fact]
    public async Task AddFile_NullFile_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.AddFile(null, null, null, CancellationToken.None);

        var error = SabnzbdResultAssert.Error(result);
        Assert.Equal("No NZB file uploaded", error.Message);
    }

    [Fact]
    public async Task DeleteFromQueue_ValidGuid_ReturnsOk()
    {
        var (service, managerProbe, _) = CreateService();
        var id = Guid.NewGuid();

        var task = service.DeleteFromQueue(id.ToString(), CancellationToken.None);
        managerProbe.ExpectMsg<DeleteDownload>(cancellationToken: TestContext.Current.CancellationToken);
        managerProbe.Reply(new DeleteDownloadResult(true, null));
        var result = await task;

        SabnzbdResultAssert.Ok(result);
    }

    [Fact]
    public async Task DeleteFromQueue_ActorReportsFailure_ReturnsError()
    {
        var (service, managerProbe, _) = CreateService();
        var id = Guid.NewGuid();

        var task = service.DeleteFromQueue(id.ToString(), CancellationToken.None);
        managerProbe.ExpectMsg<DeleteDownload>(cancellationToken: TestContext.Current.CancellationToken);
        managerProbe.Reply(new DeleteDownloadResult(false, "Not found"));
        var result = await task;

        var error = SabnzbdResultAssert.Error(result);
        Assert.Equal("Not found", error.Message);
    }

    [Fact]
    public async Task DeleteFromQueue_InvalidGuid_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.DeleteFromQueue("not-a-guid", CancellationToken.None);

        var error = SabnzbdResultAssert.Error(result);
        Assert.Equal("Item not found", error.Message);
    }

    [Fact]
    public async Task DeleteFromHistory_ValidGuid_ReturnsOk()
    {
        var (service, _, historyProbe) = CreateService();
        var id = Guid.NewGuid();

        var task = service.DeleteFromHistory(id.ToString(), CancellationToken.None);
        historyProbe.ExpectMsg<RemoveHistoryEntry>(cancellationToken: TestContext.Current.CancellationToken);
        historyProbe.Reply(new DeleteDownloadResult(true, null));
        var result = await task;

        SabnzbdResultAssert.Ok(result);
    }

    [Fact]
    public async Task DeleteFromHistory_InvalidGuid_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.DeleteFromHistory("bad", CancellationToken.None);

        var error = SabnzbdResultAssert.Error(result);
        Assert.Equal("Item not found", error.Message);
    }

    [Fact]
    public async Task Retry_Success_RemovesHistoryEntry()
    {
        var (service, managerProbe, historyProbe) = CreateService();
        var id = Guid.NewGuid();

        var task = service.Retry(id.ToString(), CancellationToken.None);
        var retryMsg = managerProbe.ExpectMsg<RetryDownload>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(id, retryMsg.DownloadId);
        managerProbe.Reply(new RetryDownloadResult(true, null));
        var result = await task;

        SabnzbdResultAssert.Ok(result);
        var removeMsg = historyProbe.ExpectMsg<RemoveHistoryEntry>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(id, removeMsg.DownloadId);
    }

    [Fact]
    public async Task Retry_Failure_PreservesHistoryEntry()
    {
        var (service, managerProbe, historyProbe) = CreateService();
        var id = Guid.NewGuid();

        var task = service.Retry(id.ToString(), CancellationToken.None);
        managerProbe.ExpectMsg<RetryDownload>(cancellationToken: TestContext.Current.CancellationToken);
        managerProbe.Reply(new RetryDownloadResult(false, "Not found"));
        var result = await task;

        var error = SabnzbdResultAssert.Error(result);
        Assert.Equal("Not found", error.Message);
        historyProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Retry_InvalidGuid_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.Retry("invalid", CancellationToken.None);

        var error = SabnzbdResultAssert.Error(result);
        Assert.Equal("Item not found", error.Message);
    }

    public async ValueTask DisposeAsync()
    {
        await _system.Terminate();
    }
}
