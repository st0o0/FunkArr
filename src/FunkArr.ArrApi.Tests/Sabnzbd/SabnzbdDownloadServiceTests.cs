using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.ArrApi.Sabnzbd;
using FunkArr.Core;
using FunkArr.Messages.Download;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
            Options.Create(new ArrApiOptions()),
            NullLogger<SabnzbdDownloadService>.Instance);

        return (service, managerProbe, historyProbe);
    }

    [Fact]
    public async Task AddFile_NullFile_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.AddFile(null, null, null, CancellationToken.None);

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("No NZB file uploaded", error.Message);
    }

    [Fact]
    public async Task DeleteFromQueue_ValidGuid_ReturnsOk()
    {
        var (service, managerProbe, _) = CreateService();
        var id = Guid.NewGuid();

        var task = service.DeleteFromQueue(id.ToString(), CancellationToken.None);
        managerProbe.ExpectMsg<DeleteDownload>();
        managerProbe.Reply(new DeleteDownloadResult(true, null));
        var result = await task;

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    [Fact]
    public async Task DeleteFromQueue_ActorReportsFailure_ReturnsError()
    {
        var (service, managerProbe, _) = CreateService();
        var id = Guid.NewGuid();

        var task = service.DeleteFromQueue(id.ToString(), CancellationToken.None);
        managerProbe.ExpectMsg<DeleteDownload>();
        managerProbe.Reply(new DeleteDownloadResult(false, "Not found"));
        var result = await task;

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Not found", error.Message);
    }

    [Fact]
    public async Task DeleteFromQueue_InvalidGuid_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.DeleteFromQueue("not-a-guid", CancellationToken.None);

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Item not found", error.Message);
    }

    [Fact]
    public async Task DeleteFromHistory_ValidGuid_ReturnsOk()
    {
        var (service, _, historyProbe) = CreateService();
        var id = Guid.NewGuid();

        var task = service.DeleteFromHistory(id.ToString(), CancellationToken.None);
        historyProbe.ExpectMsg<RemoveHistoryEntry>();
        historyProbe.Reply(new DeleteDownloadResult(true, null));
        var result = await task;

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    [Fact]
    public async Task DeleteFromHistory_InvalidGuid_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.DeleteFromHistory("bad", CancellationToken.None);

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Item not found", error.Message);
    }

    [Fact]
    public async Task Retry_ValidGuid_ReturnsOk()
    {
        var (service, managerProbe, historyProbe) = CreateService();
        var id = Guid.NewGuid();

        var task = service.Retry(id.ToString(), CancellationToken.None);
        historyProbe.ExpectMsg<RemoveHistoryEntry>();
        var retryMsg = managerProbe.ExpectMsg<RetryDownload>();
        Assert.Equal(id, retryMsg.DownloadId);
        managerProbe.Reply(new RetryDownloadResult(true, null));
        var result = await task;

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    [Fact]
    public async Task Retry_InvalidGuid_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.Retry("invalid", CancellationToken.None);

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Item not found", error.Message);
    }

    [Fact]
    public async Task SetPriority_NormalPriority_ReturnsOk()
    {
        var (service, managerProbe, _) = CreateService();
        var id = Guid.NewGuid();

        var task = service.SetPriority(id.ToString(), "0", CancellationToken.None);
        managerProbe.ExpectMsg<SetDownloadPriority>();
        managerProbe.Reply(new SetDownloadPriorityCompleted());
        var result = await task;

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    [Fact]
    public async Task SetPriority_ForceStart_SendsForceStartDownload()
    {
        var (service, managerProbe, _) = CreateService();
        var id = Guid.NewGuid();

        var task = service.SetPriority(id.ToString(), "2", CancellationToken.None);
        var forceMsg = managerProbe.ExpectMsg<ForceStartDownload>();
        Assert.Equal(id, forceMsg.DownloadId);
        managerProbe.Reply(new ForceStartDownloadResult(true, null));
        var result = await task;

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    [Fact]
    public async Task SetPriority_InvalidGuid_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.SetPriority("bad", "0", CancellationToken.None);

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Invalid nzo_id", error.Message);
    }

    [Fact]
    public async Task SetPriority_InvalidPriorityValue_ReturnsError()
    {
        var (service, _, _) = CreateService();
        var id = Guid.NewGuid();

        var result = await service.SetPriority(id.ToString(), "abc", CancellationToken.None);

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Invalid priority value", error.Message);
    }

    [Fact]
    public async Task SetPriority_Failed_ReturnsError()
    {
        var (service, managerProbe, _) = CreateService();
        var id = Guid.NewGuid();

        var task = service.SetPriority(id.ToString(), "1", CancellationToken.None);
        managerProbe.ExpectMsg<SetDownloadPriority>();
        managerProbe.Reply(new SetDownloadPriorityFailed("Download not found"));
        var result = await task;

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Download not found", error.Message);
    }

    [Fact]
    public async Task Swap_ValidGuids_ReturnsOk()
    {
        var (service, managerProbe, _) = CreateService();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var task = service.Swap(id1.ToString(), id2.ToString(), CancellationToken.None);
        var swapMsg = managerProbe.ExpectMsg<SwapDownloads>();
        Assert.Equal(id1, swapMsg.DownloadId1);
        Assert.Equal(id2, swapMsg.DownloadId2);
        managerProbe.Reply(new SwapDownloadsCompleted());
        var result = await task;

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    [Fact]
    public async Task Swap_InvalidGuids_ReturnsError()
    {
        var (service, _, _) = CreateService();

        var result = await service.Swap("bad1", "bad2", CancellationToken.None);

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Invalid nzo_id", error.Message);
    }

    [Fact]
    public async Task Swap_Failed_ReturnsError()
    {
        var (service, managerProbe, _) = CreateService();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var task = service.Swap(id1.ToString(), id2.ToString(), CancellationToken.None);
        managerProbe.ExpectMsg<SwapDownloads>();
        managerProbe.Reply(new SwapDownloadsFailed("Items not adjacent"));
        var result = await task;

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Items not adjacent", error.Message);
    }

    public async ValueTask DisposeAsync()
    {
        await _system.Terminate();
    }
}
